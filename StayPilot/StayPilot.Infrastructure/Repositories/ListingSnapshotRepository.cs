using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Domain.Entities;
using StayPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace StayPilot.Infrastructure.Repositories
{
    public class ListingSnapshotRepository : IListingSnapshotRepository
    {
        private readonly StayPilotDbContext _context;

        public ListingSnapshotRepository(StayPilotDbContext context)
        {
            _context = context;
        }

        public async Task<ListingSnapshot> AddListingSnapshotAsync(ListingSnapshot listingSnapshot)
        {
            var entry = await _context.ListingSnapshots.AddAsync(listingSnapshot);
            return entry.Entity;
        }

        public async Task<ListingSnapshot?> GetListingSnapshotByPropertyIdAsync(int propertyListingId)
        {
            return await _context.ListingSnapshots.OrderByDescending(x => x.SnapshotDateUtc).FirstOrDefaultAsync(x => x.PropertyListingId == propertyListingId) ?? null;
        }
    }
}
