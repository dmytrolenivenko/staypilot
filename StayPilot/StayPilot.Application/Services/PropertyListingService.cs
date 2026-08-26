using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Contracts.Response.Base;
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;
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
        /// </summary>
        public async Task<PropertyListingResponse> GetPropertyListingByIdAsync(int propertyId)
        {
            // Read the property and its snapshot (price, photos, etc.) from the database.
            var property = await _propertyListingRepo.GetPropertyListingByIdAsync(propertyId);

            var listing = await _listingSnapshotRepo.GetListingSnapshotByPropertyIdAsync(propertyId);

            // No property with this Id -> say so, instead of handing back an empty one.
            if (property == null)
            {
                var notFound = new PropertyListingResponse();
                notFound.AddError(ErrorCode.PropertyListingNotFound, propertyId.ToString());

                return notFound;
            }

            // Change the database data into the shape we send back to the caller.
            return Converter.MapToResponse(property, listing);
        }

        /// <summary>
        /// Save many listings in one call.
        /// Every listing is checked first. Only the ones that pass are sent to the database,
        /// the rest come back in Errors with the reason and are never saved.
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
                TotalReceived = request.Items.Count
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
                        response.AddError(error);
                        continue;
                    }

                    var result = await AddPropertyListingAsync(item, marketAreas, beaches, existingByUrl);

                    // Only the listings we actually queued for the database. An unchanged one
                    // wrote nothing, so a failed batch must not report it as a listing we lost.
                    if (result.IsNew)
                    {
                        added++;
                        readyToSave.Add(item);
                    }
                    else if (result.IsSnapshotUpdated)
                    {
                        snapshotUpdated++;
                        readyToSave.Add(item);
                    }
                    else
                    {
                        unchanged++;
                    }
                }

                // These queued nothing for the database, so whether the save works cannot change
                // them. Counted straight away, or a failed batch would leave them out of every
                // number in the response and the totals would not add up to what was sent.
                response.Unchanged += unchanged;

                try
                {
                    // One save for the whole batch, so it either all lands or none of it does.
                    await _propertyListingRepo.SaveChangesAsync();

                    response.TotalAdded += added;
                    response.SnapShotUpdated += snapshotUpdated;
                }
                catch (Exception ex)
                {
                    // Nothing from this batch landed. Report all of it instead of letting the
                    // exception kill the whole request. This catch stays on purpose: only the
                    // database can tell us a row it already accepted cannot be written, and the
                    // batches after this one still deserve their chance to save.
                    //
                    // For that to be true we have to undo this batch first. Its rows are still
                    // queued in the context, so the next save would send them again and fail on
                    // the same one, taking the rest of the upload down with it.
                    UndoFailedBatch(existingByUrl);

                    foreach (var item in readyToSave)
                    {
                        response.AddError(ErrorCode.ListingNotSaved, item.SourceUrl, ex.Message);
                    }
                }
            }

            return response;
        }

        /// <summary>
        /// Put memory back to what the database really holds, after a batch failed to save.
        ///
        /// A row that was never written still has Id 0, and that is how we find what this batch
        /// added: the snapshots queued against listings we already had, and the listings we only
        /// knew about because this batch created them. Leaving either behind would make the next
        /// batch believe a price was saved when it was not.
        /// </summary>
        private void UndoFailedBatch(Dictionary<string, PropertyListing> existingByUrl)
        {
            _propertyListingRepo.DiscardPendingChanges();

            foreach (var listing in existingByUrl.Values)
            {
                listing.ListingSnapshots.RemoveAll(x => x.Id == 0);
            }

            var neverSaved = existingByUrl
                .Where(x => x.Value.Id == 0)
                .Select(x => x.Key)
                .ToList();

            foreach (var url in neverSaved)
            {
                existingByUrl.Remove(url);
            }
        }

        /// <summary>
        /// Check one incoming listing before we try to save it, and fill in its market area Id
        /// when the caller did not send one.
        /// Returns null when the listing is good to go, or the error to report when it is not.
        /// </summary>
        private static Error? ValidateAndSetMarketArea(PropertyListingRequest propertyListing, List<MarketArea> marketAreas, Dictionary<string, PropertyListing> existingByUrl)
        {
            // A listing we already have only ever gets a new snapshot, so it needs nothing else.
            if (existingByUrl.ContainsKey(propertyListing.SourceUrl))
            {
                return null;
            }

            // Typology 0 means nothing was sent, or a name we do not know. Stored, it serialises
            // as a bare number instead of a name and blanks whole screens on the way out.
            if (propertyListing.Typology == Typology.Unknown)
            {
                return new Error(ErrorCode.ListingTypologyRequired, propertyListing.SourceUrl);
            }

            // Use the market area the caller sent, or find it from the address. We keep it on
            // the request so the save step does not have to look it up all over again.
            // GetMarketId gives back null when the address matches nothing, and a null Id fails
            // the check below like any other Id we do not have.
            propertyListing.MarketAreaId ??= Calculator.GetMarketId(marketAreas, propertyListing.Country, propertyListing.District, propertyListing.Municipality, propertyListing.Town, propertyListing.Zone);

            if (marketAreas.All(x => x.Id != propertyListing.MarketAreaId))
            {
                var address = Calculator.DescribeAddress(propertyListing.Country, propertyListing.District, propertyListing.Municipality, propertyListing.Town, propertyListing.Zone);

                return new Error(ErrorCode.ListingMarketAreaNotFound, propertyListing.SourceUrl, address);
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
                var distanceToBeachMeters = Calculator.CalculateDistanceMeters((double)propertyListing.Latitude, (double)propertyListing.Longitude, (double)closesBeach.Latitude, (double)closesBeach.Longitude);

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
            var response = new FilterPropertyListingResponse();

            // An inverted bound matches nothing, so without this the screen shows the same
            // "No listings match" it shows for a search that ran fine and found nothing - and
            // the user has no way to tell a typo from a genuinely empty market.
            if (request.MinPrice > request.MaxPrice)
            {
                response.AddError(ErrorCode.FilterRangeInverted, "price");
            }

            if (request.MinAreaM2 > request.MaxAreaM2)
            {
                response.AddError(ErrorCode.FilterRangeInverted, "area");
            }

            if (request.MinPricePerM2 > request.MaxPricePerM2)
            {
                response.AddError(ErrorCode.FilterRangeInverted, "price per m2");
            }

            if (!response.Succeeded)
            {
                return response;
            }

            // Ask the database for this page of properties and the total number of matches.
            var (items, totalRecords) = await _propertyListingRepo.FilterPropertyAsync(request);

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
