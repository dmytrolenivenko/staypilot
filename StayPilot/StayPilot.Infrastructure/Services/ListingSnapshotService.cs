
using Microsoft.EntityFrameworkCore;
using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Helpers.Mappers;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Application.Interfaces.Services;
using StayPilot.Infrastructure.Persistence;

namespace StayPilot.Infrastructure.Services
{
    public class ListingSnapshotService : IListingSnapshotService
    {
        private readonly IListingSnapshotRepository _listingSnapshotRepo;

        public ListingSnapshotService(IListingSnapshotRepository listingSnapshotRepo)
        {
            _listingSnapshotRepo = listingSnapshotRepo;
        }

        public async Task<ListingSnapshotResponse> CreateListingSnapshotAsync(ListingSnapshotRequest request)
        {
            var snapshot = Converter.MapToEntity(request);
            await _listingSnapshotRepo.AddListingSnapshotAsync(snapshot);
            return Converter.MapEntityToResponse(snapshot);
        }

        public async Task<ListingSnapshotResponse> GetListingSnapshotByPropertyIdAsync(int propertyListingId)
        {
            var snapshot = await _listingSnapshotRepo.GetListingSnapshotByPropertyIdAsync(propertyListingId);

            if (snapshot == null)
            {
                throw new KeyNotFoundException($"Snapshot with Property ID {propertyListingId} not found.");
            }
            return Converter.MapEntityToResponse(snapshot);
        }

    }
}
