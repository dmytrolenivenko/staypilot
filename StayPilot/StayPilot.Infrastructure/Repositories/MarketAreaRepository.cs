using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Domain.Entities;
using StayPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace StayPilot.Infrastructure.Repositories
{
    /// <summary>
    /// Talks to the database for market areas (country / district / town / zone).
    /// </summary>
    public class MarketAreaRepository : IMarketAreaRepository
    {
        private readonly StayPilotDbContext _context;

        public MarketAreaRepository(StayPilotDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Reads all market areas from the database.
        /// </summary>
        public async Task<List<MarketArea>> GetAllMarketAreasAsync()
        {
            return await _context.MarketAreas.ToListAsync();
        }

        /// <summary>
        /// Reads the choices for the next drop-down in the location filter.
        /// You pass what is already picked; it returns the next level down.
        /// </summary>
        public async Task<List<string>> GetMarketAreaOptionsAsync(string? district, string? municipality, string? town)
        {
            var query = _context.MarketAreas.AsQueryable();

            // Town is picked -> return its zones (only rows that have a zone). No repeats, sorted.
            if (!string.IsNullOrWhiteSpace(town))
            {
                return await query.Where(x => x.Town == town && x.Zone != null).Select(x => x.Zone).Distinct().OrderBy(x => x).ToListAsync();
            }
            // Municipality is picked -> return its towns.
            else if (!string.IsNullOrWhiteSpace(municipality))
            {
                return await query.Where(x => x.Municipality == municipality).Select(x => x.Town).Distinct().OrderBy(x => x).ToListAsync();
            }
            // District is picked -> return its municipalities.
            else if (!string.IsNullOrWhiteSpace(district))
            {
                return await query.Where(x => x.District == district).Select(x => x.Municipality).Distinct().OrderBy(x => x).ToListAsync();
            }

            // Nothing picked yet -> return the top level (all municipalities), de-duplicated and sorted.
            return await _context.MarketAreas.Select(x => x.Municipality).Distinct().OrderBy(x => x).ToListAsync();
        }
    }
}
