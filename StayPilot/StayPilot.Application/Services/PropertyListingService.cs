using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Domain.Entities;
using System.Globalization;
using System.Text;
using StayPilot.Application.Helpers.Mappers;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Application.Interfaces.Services;

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
                samePrice = lastListing.Price == propertyListing.ListingSnapshot.Price;
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
            property.MarketAreaId = propertyListing.MarketAreaId ?? GetMarketId(marketAreasRepo, propertyListing);
            property.MarketArea = marketAreasRepo.FirstOrDefault(x => x.Id == property.MarketAreaId) ?? throw new InvalidOperationException("MarketArea can not be null");

            // Location is required. Stop if it is missing.
            if (propertyListing.Latitude == null || propertyListing.Longitude == null)
            {
                throw new InvalidOperationException("Latitude and Longitude must be provided for the property listing.");
            }

            // Find the nearest beach to this property.
            var closesBeach = GetTheClosestBeach(beachesRepo, propertyListing);

            // If we found a beach, save its name and how far it is (in meters).
            if (closesBeach is not null)
            {
                var distanceToBeachMeters = CalculateDistanceMeters((double)propertyListing.Latitude.Value, (double)propertyListing.Longitude.Value, (double)closesBeach.Latitude, (double)closesBeach.Longitude);

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
        public async Task<ListPropertyListingResponse> FilterPropertyAsync(ListPropertyListingRequest request)
        {
            // Ask the database for this page of properties and the total number of matches.
            var (items, totalRecords) = await _propertyListingRepo.FilterPropertyAsync(request);

            var response = new ListPropertyListingResponse();

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

        /// <summary>
        /// Clean text so two names can be compared safely.
        /// It makes the text lower case, removes accents (á becomes a), and trims spaces.
        /// Example: "  Faró " becomes "faro".
        /// </summary>
        private static string NormalizeText(string value)
        {
            // Empty or spaces only -> return empty text.
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            // Lower case, no spaces at the ends, and split each accent from its letter.
            var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);

            var builder = new StringBuilder();

            // Keep every character, but drop the accent marks.
            foreach (var c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);

                if (category != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(c);
                }
            }

            // Put the text back together in the normal form.
            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Find which market area a property belongs to, using its address.
        /// We try an exact match first. If that fails, we try easier matches.
        /// </summary>
        private static int GetMarketId(List<MarketArea> marketAreas, PropertyListingRequest propertyListing)
        {
            // Clean each address part so we can compare it safely (see NormalizeText).
            var country = NormalizeText(propertyListing.Country);
            var district = NormalizeText(propertyListing.District);
            var municipality = NormalizeText(propertyListing.Municipality);
            var town = NormalizeText(propertyListing.Town);
            var zone = NormalizeText(propertyListing.Zone ?? string.Empty);

            // Try 1: exact match on all parts (country, district, municipality, town, zone).
            var marketArea = marketAreas.FirstOrDefault(x =>
                NormalizeText(x.Country) == country &&
                NormalizeText(x.District) == district &&
                NormalizeText(x.Municipality) == municipality &&
                NormalizeText(x.Town) == town &&
                NormalizeText(x.Zone ?? string.Empty) == zone);

            // Try 2: many listings have no Zone (the source does not give it).
            // So match by Town only. Prefer a market area that also has no Zone.
            marketArea ??= marketAreas
                .Where(x =>
                    NormalizeText(x.Country) == country &&
                    NormalizeText(x.District) == district &&
                    NormalizeText(x.Municipality) == municipality &&
                    NormalizeText(x.Town) == town)
                .OrderBy(x => x.Zone == null ? 0 : 1)
                .ThenBy(x => x.Id)
                .FirstOrDefault();

            // Try 3: some listings only give the Municipality (no Town).
            // So match by Municipality only. Someone can fix the exact zone later by hand.
            marketArea ??= marketAreas
                .Where(x =>
                    NormalizeText(x.Country) == country &&
                    NormalizeText(x.District) == district &&
                    NormalizeText(x.Municipality) == municipality)
                .OrderBy(x => x.Zone == null ? 0 : 1)
                .ThenBy(x => x.Id)
                .FirstOrDefault();

            // Still nothing -> we cannot place this property. Stop with an error.
            if (marketArea == null)
                throw new InvalidOperationException("Market area not found. Property URL: " + propertyListing.SourceUrl);

            return marketArea.Id;
        }

        /// <summary>
        /// Find the beach nearest to the property.
        /// Returns null if the property has no location.
        /// </summary>
        private static BeachMarker? GetTheClosestBeach(List<BeachMarker> beaches, PropertyListingRequest propertyListing)
        {
            // No location -> we cannot measure distance, so no beach.
            if (propertyListing.Latitude == null || propertyListing.Longitude == null)
            {
                return null;
            }

            var propertyLat = (double)propertyListing.Latitude.Value;
            var propertyLon = (double)propertyListing.Longitude.Value;

            // Sort all beaches by distance to the property and take the closest one.
            var closestBeach = beaches
                .OrderBy(beach => CalculateDistanceMeters(
                    propertyLat,
                    propertyLon,
                    (double)beach.Latitude,
                    (double)beach.Longitude))
                .FirstOrDefault();

            return closestBeach;
        }

        /// <summary>
        /// Distance in meters between two points on Earth (given as latitude/longitude).
        /// </summary>
        private static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
        {
            // Haversine formula: the standard way to measure distance on a globe.
            const double earthRadiusMeters = 6371000;

            double ToRadians(double degrees)
            {
                return degrees * Math.PI / 180;
            }

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) *
                Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) *
                Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusMeters * c;
        }

    }
}
