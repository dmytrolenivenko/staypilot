using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;

namespace StayPilot.Application.Interfaces.Services
{
    public interface IListingSnapshotService
    {
        Task<ListingSnapshotResponse> CreateListingSnapshotAsync(ListingSnapshotRequest request);

        Task<ListingSnapshotResponse> GetListingSnapshotByPropertyIdAsync(int propertyListingId);
    }
}
