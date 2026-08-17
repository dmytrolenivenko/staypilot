using Microsoft.EntityFrameworkCore;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;
using StayPilot.Infrastructure.Persistence;

namespace StayPilot.Infrastructure.Repositories
{
    /// <inheritdoc/>
    public class MarketAreaStatsRepository : IMarketAreaStatsRepository
    {
        private readonly StayPilotDbContext _context;

        public MarketAreaStatsRepository(StayPilotDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<List<MarketAreaStats>> GetAllMarketAreaStatsAsync()
        {
            return await _context.MarketAreaStats.ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<MarketAreaStats>> GetLeaderboardAsync(AreaLevel level, int minListings)
        {
            // The sample gate lives here rather than in the calculator on purpose: a thin place
            // is still a true row, so we save it and decide at read time whether to show it.
            //
            // Priciest first is only a stable default so the payload never comes back shuffled.
            // The browser re-sorts it however the user clicks.
            return await _context.MarketAreaStats
                .Where(x => x.Level == level && x.ListingCount >= minListings)
                .OrderByDescending(x => x.MedianPricePerM2)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task AddMarketAreaStatsAsync(IEnumerable<MarketAreaStats> stats)
        {
            await _context.MarketAreaStats.AddRangeAsync(stats);
        }

        /// <inheritdoc/>
        public void RemoveMarketAreaStats(IEnumerable<MarketAreaStats> stats)
        {
            _context.MarketAreaStats.RemoveRange(stats);
        }

        /// <inheritdoc/>
        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
