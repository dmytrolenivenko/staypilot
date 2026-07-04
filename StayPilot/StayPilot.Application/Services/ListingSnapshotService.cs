
using Microsoft.EntityFrameworkCore;
using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Interfaces;
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;
using StayPilot.Infrastructure.Persistence;

namespace StayPilot.Application.Services
{
    public class ListingSnapshotService : IListingSnapshotService
    {
        private readonly StayPilotDbContext _context;

        public ListingSnapshotService(StayPilotDbContext context)
        {
            _context = context;
        }

        public async Task<ListingSnapshotResponse> CreateSnapshotAsync(ListingSnapshotRequest request)
        {
            var snapshot = MapToEntity(request);
            await _context.ListingSnapshots.AddAsync(snapshot);
            await _context.SaveChangesAsync();

            return MapEntityToResponse(snapshot);
        }

        public async Task<ListingSnapshotResponse> GetListingSnapshotAsync(int snapshotId)
        {
            var snapshot = await _context.ListingSnapshots.FirstOrDefaultAsync(x => x.Id == snapshotId);
            if (snapshot == null)
            {
                throw new KeyNotFoundException($"Snapshot with ID {snapshotId} not found.");
            }
            return MapEntityToResponse(snapshot);
        }

        public ListingSnapshot MapToEntity(ListingSnapshotRequest snapshot)
        {
            return new ListingSnapshot
            {
                PropertyListingId = snapshot.PropertyListingId,
                Price = snapshot.Price,
                PricePerM2 = snapshot.PricePerM2,
                Status = snapshot.Status,
                SnapshotDateUtc = snapshot.SnapshotDateUtc
            };
        }

        public ListingSnapshotResponse MapEntityToResponse(ListingSnapshot snapshot)
        {
            return new ListingSnapshotResponse
            {
                Id = snapshot.Id,
                PropertyListingId = snapshot.PropertyListingId,
                Price = snapshot.Price,
                PricePerM2 = snapshot.PricePerM2,
                Status = snapshot.Status,
                SnapshotDateUtc = snapshot.SnapshotDateUtc
            };
        }

    }
}
