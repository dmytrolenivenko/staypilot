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

            // First narrow by everything already picked. Names repeat across the country
            // (six different municipalities have a freguesia called "Pinheiro"), so each
            // level has to be filtered by its parents too, not only by itself.
            if (!string.IsNullOrWhiteSpace(district))
            {
                query = query.Where(x => x.District == district);
            }

            if (!string.IsNullOrWhiteSpace(municipality))
            {
                query = query.Where(x => x.Municipality == municipality);
            }

            if (!string.IsNullOrWhiteSpace(town))
            {
                query = query.Where(x => x.Town == town);
            }

            // Then return the level just below the deepest one picked.
            // Town is picked -> its zones (only rows that have a zone). No repeats, sorted.
            if (!string.IsNullOrWhiteSpace(town))
            {
                return await query.Where(x => x.Zone != null).Select(x => x.Zone!).Distinct().OrderBy(x => x).ToListAsync();
            }
            // Municipality is picked -> its towns.
            if (!string.IsNullOrWhiteSpace(municipality))
            {
                return await query.Select(x => x.Town).Distinct().OrderBy(x => x).ToListAsync();
            }
            // District is picked -> its municipalities.
            if (!string.IsNullOrWhiteSpace(district))
            {
                return await query.Select(x => x.Municipality).Distinct().OrderBy(x => x).ToListAsync();
            }

            // Nothing picked yet -> the top level: the districts.
            return await query.Select(x => x.District).Distinct().OrderBy(x => x).ToListAsync();
        }
    }
}
