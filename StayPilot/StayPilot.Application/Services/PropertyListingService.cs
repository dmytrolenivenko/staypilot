using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Domain.Entities;
using System.Globalization;
using System.Text;
using StayPilot.Application.Helpers.Mappers;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Application.Interfaces.Services;
using StayPilot.Application.Helpers.Calculators;

namespace StayPilot.Application.Services
{
    public class PropertyListingService : IPropertyListingService
    {
        private readonly IPropertyListingRepository _propertyListingRepo;
        private readonly IMarketAreaRepository _marketAreaRepo;
        private readonly IBeachMarkerRepository _beachMarkerRepo;
        private readonly IListingSnapshotRepository _listingSnapshotRepo;

        public PropertyListingService(IPropertyListingRepository propertyListingRepo, IMarketAreaRepository marketAreaRepo, IBeachMarkerRepository beachMarkerRepo, IListingSnapshotRepository listingSnapshotRepo)
        {
            _propertyListingRepo = propertyListingRepo;
            _marketAreaRepo = marketAreaRepo;
            _beachMarkerRepo = beachMarkerRepo;
            _listingSnapshotRepo = listingSnapshotRepo;
        }

        /// <summary>
        /// Get one property by its Id.
        /// Returns null if the property does not exist.
        /// </summary>
        public async Task<PropertyListingResponse?> GetPropertyListingByIdAsync(int propertyId)
        {
            // Read the property and its snapshot (price, photos, etc.) from the database.
            var property = await _propertyListingRepo.GetPropertyListingByIdAsync(propertyId);

            var listing = await _listingSnapshotRepo.GetListingSnapshotByPropertyIdAsync(propertyId);

            // No property with this Id -> nothing to return.
            if (property == null)
            {
                return null;
            }

            // Change the database data into the shape we send back to the caller.
            return Converter.MapToResponse(property, listing);
        }


        /// <summary>
        /// Save a new property.
        /// If the same property already exists (same URL), we do not save it again.
        /// We also set its market area and its closest beach.
        /// </summary>
        public async Task<PropertyListingResponse> AddPropertyListingAsync(PropertyListingRequest propertyListing)
        {
            // Load all market areas and all beaches once. We use these lists below.
            var marketAreasRepo = await _marketAreaRepo.GetAllMarketAreasAsync();
            var beachesRepo = await _beachMarkerRepo.GetAllBeachMarkersAsync();

            // Is this property already saved? We check by its URL.
            var propertyExist = await _propertyListingRepo.GetPropertyListingByUrlAsync(propertyListing.SourceUrl);
            var samePrice = false;

            // If property exist, let's check if the price changed
            if (propertyExist is not null)
            {
                var lastListing = await _listingSnapshotRepo.GetListingSnapshotByPropertyIdAsync(propertyExist.Id);

                // lastListing can be null if the existing property has no snapshot yet.
                // Treat "no previous snapshot" as "price changed" so we add one below.
                samePrice = lastListing is not null && lastListing.Price == propertyListing.ListingSnapshot.Price;
            }

            // Already there -> return the existing one and stop.
            if (propertyExist is not null && samePrice)
            {
                return Converter.MapToResponse(propertyExist);
            }

            // If the property exists, but the price is differnt update ONLY Snapshot
            if (propertyExist is not null && !samePrice)
            {
                var newListingSnapshot = Converter.MapToEntity(propertyListing.ListingSnapshot);
                newListingSnapshot.PropertyListing = propertyExist;
                await _listingSnapshotRepo.AddListingSnapshotAsync(newListingSnapshot);
                await _propertyListingRepo.SaveChangesAsync();
                return Converter.MapToResponse(propertyExist, newListingSnapshot);
            }

            // Build the entity from the request.
            var property = Converter.MapToEntity(propertyListing);

            // Use the market area sent by the caller, or find it from the address.
            property.MarketAreaId = propertyListing.MarketAreaId ?? Calculator.GetMarketId(marketAreasRepo, propertyListing.Country, propertyListing.District, propertyListing.Municipality, propertyListing.Town, propertyListing.Zone);
            property.MarketArea = marketAreasRepo.FirstOrDefault(x => x.Id == property.MarketAreaId) ?? throw new InvalidOperationException("MarketArea can not be null");

            // Location is required. Stop if it is missing.
            if (propertyListing.Latitude == null || propertyListing.Longitude == null)
            {
                throw new InvalidOperationException("Latitude and Longitude must be provided for the property listing.");
            }

            // Find the nearest beach to this property.
            var closesBeach = Calculator.GetTheClosestBeach(beachesRepo, propertyListing.Latitude, propertyListing.Longitude);

            // If we found a beach, save its name and how far it is (in meters).
            if (closesBeach is not null)
            {
                var distanceToBeachMeters = Calculator.CalculateDistanceMeters((double)propertyListing.Latitude.Value, (double)propertyListing.Longitude.Value, (double)closesBeach.Latitude, (double)closesBeach.Longitude);

                property.NearestBeachName = closesBeach.Name;
                property.NearestBeachMarkerId = closesBeach.Id;
                property.DistanceToBeachMeters = (int)distanceToBeachMeters;
                property.DistanceToBeachMethod = "osm_center_point";
            }

            // Build the snapshot and link it to the property.
            var listingSnapshot = Converter.MapToEntity(propertyListing.ListingSnapshot);
            listingSnapshot.PropertyListing = property;

            // Save property + snapshot together, then commit to the database.
            await _propertyListingRepo.AddPropertyListingAsync(property);
            await _listingSnapshotRepo.AddListingSnapshotAsync(listingSnapshot);
            await _propertyListingRepo.SaveChangesAsync();

            return Converter.MapToResponse(property, listingSnapshot);
        }

        /// <summary>
        /// Search properties using filters, one page at a time.
        /// Returns the properties for the page and the total count of matches.
        /// </summary>
        public async Task<FilterPropertyListingResponse> FilterPropertyListingAsync(FilterPropertyListingRequest request)
        {
            // Ask the database for this page of properties and the total number of matches.
            var (items, totalRecords) = await _propertyListingRepo.FilterPropertyAsync(request);

            var response = new FilterPropertyListingResponse();

            // For each property, take its snapshot and build one item for the response.
            foreach(var property in items)
            {
                var snapshot = property.ListingSnapshots.FirstOrDefault();
                var item = Converter.MapToResponse(property, snapshot);
                response.Items.Add(item);
            }

            // Add the paging info so the caller knows how many pages exist.
            response.TotalRecords = totalRecords;
            response.PageNumber = request.PageNumber;
            response.PageSize = request.PageSize;


            return response;
        }
    }
}
