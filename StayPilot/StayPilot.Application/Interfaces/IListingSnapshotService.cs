using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;

namespace StayPilot.Application.Interfaces
{
    public interface IListingSnapshotService
    {
        Task<ListingSnapshotResponse> CreateSnapshotAsync(ListingSnapshotRequest request);

        Task<ListingSnapshotResponse> GetListingSnapshotAsync(int snapshotId);
    }
}
