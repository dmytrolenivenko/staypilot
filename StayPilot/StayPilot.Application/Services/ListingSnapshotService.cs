
using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Contracts.Response.Base;
using StayPilot.Application.Helpers.Mappers;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Application.Interfaces.Services;
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Services
{
    /// <summary>
    /// Handles listing snapshots (price, status, date) for a property.
    /// </summary>
    public class ListingSnapshotService : IListingSnapshotService
    {
        private readonly IListingSnapshotRepository _listingSnapshotRepo;
        private readonly IPropertyListingRepository _propertyListingRepo;

        public ListingSnapshotService(IListingSnapshotRepository listingSnapshotRepo, IPropertyListingRepository propertyListingRepo)
        {
            _listingSnapshotRepo = listingSnapshotRepo;
            _propertyListingRepo = propertyListingRepo;
        }

        /// <inheritdoc/>
        public async Task<ListingSnapshotResponse> CreateListingSnapshotAsync(ListingSnapshotRequest request)
        {
            // Build the entity from the request and save it.
            var snapshot = Helpers.Mappers.Converter.MapToEntity(request);
            await _listingSnapshotRepo.AddListingSnapshotAsync(snapshot);

            // Actually write it to the database. Without this, AddAsync only stages the
            // row in the change tracker and nothing is ever persisted.
            await _listingSnapshotRepo.SaveChangesAsync();

            return Helpers.Mappers.Converter.MapToResponse(snapshot);
        }

        /// <inheritdoc/>
        public async Task<ListingSnapshotResponse> GetListingSnapshotByPropertyIdAsync(int propertyListingId)
        {
            var snapshot = await _listingSnapshotRepo.GetListingSnapshotByPropertyIdAsync(propertyListingId);

            // No snapshot for this property -> tell the caller with an error.
            if (snapshot == null)
            {
                var notFound = new ListingSnapshotResponse();
                notFound.AddError(ErrorCode.SnapshotNotFound, propertyListingId.ToString());

                return notFound;
            }

            return Helpers.Mappers.Converter.MapToResponse(snapshot);
        }

        /// <inheritdoc/>
        public async Task<ReconcileActiveListingsResponse> ReconcileActiveListingsAsync(ReconcileActiveListingsRequest request)
        {
            var response = new ReconcileActiveListingsResponse();

            // An empty list would read as "nothing is live any more" and mark every active
            // listing sold in one call - almost certainly a caller bug (an empty sweep, a
            // truncated report), never a real outcome. Refuse it instead of honouring it.
            if (request.ActiveUrls.Count == 0)
            {
                response.AddError(ErrorCode.ReconcileActiveUrlsRequired);
                return response;
            }

            var stillLive = new HashSet<string>(request.ActiveUrls, StringComparer.OrdinalIgnoreCase);
            var activeListings = await _propertyListingRepo.GetActiveListingsAsync();

            response.ActiveListingsChecked = activeListings.Count;

            foreach (var listing in activeListings)
            {
                if (stillLive.Contains(listing.SourceUrl))
                {
                    continue;
                }

                // Carries the last known price forward - this snapshot records a status change,
                // not a new asking price, so there is no other price to give it.
                var lastSnapshot = listing.ListingSnapshots.OrderByDescending(x => x.SnapshotDateUtc).FirstOrDefault();

                var soldSnapshot = new ListingSnapshot
                {
                    PropertyListingId = listing.Id,
                    PropertyListing = listing,
                    Price = lastSnapshot?.Price ?? 0,
                    PricePerM2 = lastSnapshot?.PricePerM2 ?? 0,
                    Status = ListingStatus.Sold
                };

                await _listingSnapshotRepo.AddListingSnapshotAsync(soldSnapshot);

                response.MarkedSoldCount++;
                response.MarkedSoldUrls.Add(listing.SourceUrl);
            }

            await _listingSnapshotRepo.SaveChangesAsync();

            return response;
        }
    }
}
