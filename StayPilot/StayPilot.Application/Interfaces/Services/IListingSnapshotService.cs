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
        /// Throws if the property has no snapshot.
        /// </summary>
        Task<ListingSnapshotResponse> GetListingSnapshotByPropertyIdAsync(int propertyListingId);
    }
}
