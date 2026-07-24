using StayPilot.Domain.Entities;

namespace StayPilot.Application.Interfaces.Repositories
{
    /// <summary>
    /// Reads and saves listing snapshots (price, photos, status) in the database.
    /// </summary>
    public interface IListingSnapshotRepository
    {
        /// <summary>
        /// Get the snapshot of one property by the property Id.
        /// Returns null if there is none.
        /// </summary>
        Task<ListingSnapshot?> GetListingSnapshotByPropertyIdAsync(int propertyId);

        /// <summary>
        /// Add a new snapshot to the database.
        /// </summary>
        Task<ListingSnapshot> AddListingSnapshotAsync(ListingSnapshot listingSnapshot);

        /// <summary>
        /// Write all pending changes to the database.
        /// </summary>
        Task SaveChangesAsync();
    }
}
