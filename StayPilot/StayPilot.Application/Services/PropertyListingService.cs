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
        // One SaveChangesAsync per this many rows. Small enough that a database error only
        // rolls back a slice of the upload, big enough to avoid a round trip per listing.
        private const int BatchSize = 100;

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
        /// Save many listings in one call.
        /// Every listing is checked first. Only the ones that pass are sent to the database,
        /// the rest come back in FailedListings with the reason and are never saved.
        /// </summary>
        public async Task<BulkAddPropertyListingResponse> BulkAddPropertyListingAsync(BulkAddPropertyListingRequest request)
        {
            // Read the lookup tables once for the whole call. They are whole-table reads, so
            // reading them per listing is what made this slow.
            var marketAreas = await _marketAreaRepo.GetAllMarketAreasAsync();
            var beaches = await _beachMarkerRepo.GetAllBeachMarkersAsync();

            // The listings we already have, so we can tell a new one from a price change.
            var urls = request.Items.Select(x => x.SourceUrl).ToList();
            var alreadySaved = await _propertyListingRepo.GetBulkPropertyListingByUrlAsync(urls) ?? new List<PropertyListing>();
            var existingByUrl = alreadySaved.ToDictionary(x => x.SourceUrl);

            var response = new BulkAddPropertyListingResponse
            {
                TotalReceived = request.Items.Count,
                FailedListings = new Dictionary<string, string>()
            };

            // Save in small batches, so one database error only costs us that batch.
            foreach (var batch in request.Items.Chunk(BatchSize))
            {
                // The listings we built rows for. Kept so we can report them all if the save fails.
                var readyToSave = new List<PropertyListingRequest>();

                // Counted here first, and only added to the response once the save works.
                // Otherwise the counts would claim rows were saved when the batch got rolled back.
                var added = 0;
                var snapshotUpdated = 0;
                var unchanged = 0;

                foreach (var item in batch)
                {
                    var error = ValidateAndSetMarketArea(item, marketAreas, existingByUrl);

                    // Something is wrong with this listing. Report it and leave it out of the save.
                    if (error is not null)
                    {
                        response.FailedListings[item.SourceUrl] = error;
                        continue;
                    }

                    var result = await AddPropertyListingAsync(item, marketAreas, beaches, existingByUrl);

                    readyToSave.Add(item);

                    if (result.IsNew)
                    {
                        added++;
                    }
                    else if (result.IsSnapshotUpdated)
                    {
                        snapshotUpdated++;
                    }
                    else
                    {
                        unchanged++;
                    }
                }

                try
                {
                    // One save for the whole batch, so it either all lands or none of it does.
                    await _propertyListingRepo.SaveChangesAsync();

                    response.TotalAdded += added;
                    response.SnapShotUpdated += snapshotUpdated;
                    response.Unchanged += unchanged;
                }
                catch (Exception ex)
                {
                    // Nothing from this batch landed. Report all of it instead of letting the
                    // exception kill the whole request.
                    foreach (var item in readyToSave)
                    {
                        response.FailedListings[item.SourceUrl] = ex.Message;
                    }
                }
            }

            return response;
        }

        /// <summary>
        /// Check one incoming listing before we try to save it, and fill in its market area Id
        /// when the caller did not send one.
        /// Returns null when the listing is good to go, or the reason to report when it is not.
        /// </summary>
        private static string? ValidateAndSetMarketArea(PropertyListingRequest propertyListing, List<MarketArea> marketAreas, Dictionary<string, PropertyListing> existingByUrl)
        {
            // A listing we already have only ever gets a new snapshot, so it needs nothing else.
            if (existingByUrl.ContainsKey(propertyListing.SourceUrl))
            {
                return null;
            }

            if (propertyListing.Latitude is null || propertyListing.Longitude is null)
            {
                return "Latitude and Longitude are required.";
            }

            try
            {
                // Use the market area the caller sent, or find it from the address. We keep it on
                // the request so the save step does not have to look it up all over again.
                propertyListing.MarketAreaId ??= Calculator.GetMarketId(marketAreas, propertyListing.Country, propertyListing.District, propertyListing.Municipality, propertyListing.Town, propertyListing.Zone);

                if (marketAreas.All(x => x.Id != propertyListing.MarketAreaId))
                {
                    return "No market area matches this address.";
                }
            }
            catch (InvalidOperationException ex)
            {
                // GetMarketId throws when the address matches nothing. Here that is a listing we
                // reject, not a crash.
                return ex.Message;
            }

            return null;
        }

        /// <summary>
        /// Build the rows for one listing and hand them to the repositories.
        /// Nothing is written until SaveChangesAsync runs.
        /// Only call this with a listing that passed ValidateAndSetMarketArea.
        /// </summary>
        private async Task<PropertyListingResponse> AddPropertyListingAsync(PropertyListingRequest propertyListing, List<MarketArea> marketAreas, List<BeachMarker> beaches, Dictionary<string, PropertyListing> existingByUrl)
        {
            // Is this property already saved? We check by its URL.
            var propertyExist = existingByUrl.GetValueOrDefault(propertyListing.SourceUrl);
            var samePrice = false;

            // If property exist, let's check if the price changed
            if (propertyExist is not null)
            {
                var lastListing = propertyExist.ListingSnapshots.OrderByDescending(x => x.SnapshotDateUtc).FirstOrDefault();

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

                // Keep the in-memory snapshot list current so a duplicate SourceUrl later in the
                // same request sees this price instead of the stale one loaded from the database.
                propertyExist.ListingSnapshots.Add(newListingSnapshot);

                var response = Converter.MapToResponse(propertyExist, newListingSnapshot);
                response.IsSnapshotUpdated = true;
                return response;
            }

            // Build the entity from the request.
            var property = Converter.MapToEntity(propertyListing);

            // ValidateAndSetMarketArea already resolved and checked this.
            property.MarketAreaId = propertyListing.MarketAreaId!.Value;
            property.MarketArea = marketAreas.First(x => x.Id == property.MarketAreaId);

            // Find the nearest beach to this property.
            var closesBeach = Calculator.GetTheClosestBeach(beaches, propertyListing.Latitude, propertyListing.Longitude);

            // If we found a beach, save its name and how far it is (in meters).
            if (closesBeach is not null)
            {
                var distanceToBeachMeters = Calculator.CalculateDistanceMeters((double)propertyListing.Latitude!.Value, (double)propertyListing.Longitude!.Value, (double)closesBeach.Latitude, (double)closesBeach.Longitude);

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

            property.ListingSnapshots.Add(listingSnapshot);

            // Register it as "existing" right away so a duplicate SourceUrl later in the same
            // request updates this one instead of trying to insert the same URL twice.
            existingByUrl[propertyListing.SourceUrl] = property;

            var newPropertyResponse = Converter.MapToResponse(property, listingSnapshot);
            newPropertyResponse.IsNew = true;
            return newPropertyResponse;
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
