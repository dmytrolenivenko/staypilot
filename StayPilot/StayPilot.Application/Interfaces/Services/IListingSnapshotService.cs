using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;

namespace StayPilot.Application.Interfaces.Services
{
    /// <summary>
    /// Handles listing snapshots (price, status, date) for a property.
    /// </summary>
    public interface IListingSnapshotService
    {
        /// <summary>
        /// Save a new snapshot and return it in the shape we send back.
        /// </summary>
        Task<ListingSnapshotResponse> CreateListingSnapshotAsync(ListingSnapshotRequest request);

        /// <summary>
        /// Get the snapshot of one property by the property Id.
        /// Comes back carrying SnapshotNotFound when the property has no snapshot.
        /// </summary>
        Task<ListingSnapshotResponse> GetListingSnapshotByPropertyIdAsync(int propertyListingId);

        /// <summary>
        /// Compares ActiveUrls against every listing this API holds as Active, and adds a new
        /// Sold snapshot for each one missing from that list. Meant to run right after a full
        /// sweep of the source site, using the URLs it actually saw - nothing is deleted, the
        /// listing and its price history stay, only a new snapshot records it as sold.
        /// Comes back carrying ReconcileActiveUrlsRequired, unchanged, if ActiveUrls is empty.
        /// </summary>
        Task<ReconcileActiveListingsResponse> ReconcileActiveListingsAsync(ReconcileActiveListingsRequest request);
    }
}
