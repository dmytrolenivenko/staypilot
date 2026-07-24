using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Domain.Entities;
using StayPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace StayPilot.Infrastructure.Repositories
{
    /// <summary>
    /// Talks to the database for listing snapshots (price and state at a point in time).
    /// </summary>
    public class ListingSnapshotRepository : IListingSnapshotRepository
    {
        private readonly StayPilotDbContext _context;

        public ListingSnapshotRepository(StayPilotDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Adds a new snapshot. It is only kept in memory here;
        /// SaveChanges (called elsewhere) writes it to the database.
        /// </summary>
        public async Task<ListingSnapshot> AddListingSnapshotAsync(ListingSnapshot listingSnapshot)
        {
            var entry = await _context.ListingSnapshots.AddAsync(listingSnapshot);
            return entry.Entity;
        }

        /// <summary>
        /// Reads the newest snapshot of one property. Returns null if it has none.
        /// </summary>
        public async Task<ListingSnapshot?> GetListingSnapshotByPropertyIdAsync(int propertyListingId)
        {
            // Sort by date, newest first, then take the first match for this property.
            return await _context.ListingSnapshots.OrderByDescending(x => x.SnapshotDateUtc).FirstOrDefaultAsync(x => x.PropertyListingId == propertyListingId);
        }

        /// <summary>
        /// Writes all pending changes to the database.
        /// </summary>
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
