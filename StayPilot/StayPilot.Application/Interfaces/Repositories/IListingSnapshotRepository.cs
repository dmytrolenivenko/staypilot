using StayPilot.Domain.Entities;

namespace StayPilot.Application.Interfaces.Repositories
{
    public interface IListingSnapshotRepository
    {
        Task<ListingSnapshot?> GetListingSnapshotByPropertyIdAsync(int propertyId);

        Task<ListingSnapshot> AddListingSnapshotAsync(ListingSnapshot listingSnapshot);
    }
}
