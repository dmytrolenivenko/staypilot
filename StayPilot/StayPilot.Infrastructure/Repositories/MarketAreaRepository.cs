using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Domain.Entities;
using StayPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using StayPilot.Application.Contracts.Request;
using StayPilot.Domain.Enums;

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
            // Nothing here is written back through this read - tracking the whole table costs
            // more than reading it.
            return await _context.MarketAreas.AsNoTracking().ToListAsync();
        }

        /// <summary>
        /// Reads one market area by id.
        /// </summary>
        public async Task<MarketArea?> GetMarketAreaByIdAsync(int id)
        {
            return await _context.MarketAreas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        }

        /// <summary>
        /// Reads one page of market areas from the database, plus the total number of matches.
        /// </summary>
        public async Task<(List<MarketArea> Items, int TotalRecords)> GetMarketAreasPageAsync(MarketAreaRequest request)
        {
            var query = _context.MarketAreas.AsQueryable();

            // Optional search: match any part of the name on any of the address levels.
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();

                query = query.Where(x =>
                    EF.Functions.Like(x.District, $"%{search}%") ||
                    EF.Functions.Like(x.Municipality, $"%{search}%") ||
                    EF.Functions.Like(x.Town, $"%{search}%") ||
                    (x.Zone != null && EF.Functions.Like(x.Zone, $"%{search}%")));
            }

            // Count before paging, so the caller knows how many pages exist.
            var totalRecords = await query.CountAsync();

            var ordered = Order(query, request.SortBy, request.SortDescending);

            var items = await ordered
                // Paging needs a stable order, otherwise the same row can show up on two pages.
                // Names repeat all over the country, so Id is what finally settles the ties.
                .ThenBy(x => x.Id)
                .Skip((request.PageNumber - 1) * request.PageSize) // jump over the earlier pages
                .Take(request.PageSize)                            // take only this page
                .ToListAsync();

            return (items, totalRecords);
        }

        /// <summary>
        /// Sorts the page by the column the caller clicked. Every column the table shows can
        /// be sorted, in both directions; the caller adds the tie-breaker.
        /// </summary>
        private static IOrderedQueryable<MarketArea> Order(IQueryable<MarketArea> query, MarketAreaSortBy sortBy, bool descending)
        {
            return sortBy switch
            {
                MarketAreaSortBy.Id => descending ? query.OrderByDescending(x => x.Id) : query.OrderBy(x => x.Id),

                MarketAreaSortBy.District => descending ? query.OrderByDescending(x => x.District) : query.OrderBy(x => x.District),

                MarketAreaSortBy.Municipality => descending ? query.OrderByDescending(x => x.Municipality) : query.OrderBy(x => x.Municipality),

                MarketAreaSortBy.Town => descending ? query.OrderByDescending(x => x.Town) : query.OrderBy(x => x.Town),

                MarketAreaSortBy.Zone => descending ? query.OrderByDescending(x => x.Zone) : query.OrderBy(x => x.Zone),

                MarketAreaSortBy.Country => descending ? query.OrderByDescending(x => x.Country) : query.OrderBy(x => x.Country),

                MarketAreaSortBy.Notes => descending ? query.OrderByDescending(x => x.Notes) : query.OrderBy(x => x.Notes),

                // The address order the table reads in, and what it falls back to.
                _ => descending
                    ? query.OrderByDescending(x => x.District).ThenByDescending(x => x.Municipality).ThenByDescending(x => x.Town)
                    : query.OrderBy(x => x.District).ThenBy(x => x.Municipality).ThenBy(x => x.Town)
            };
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
                var towns = await query.Select(x => x.Town).Distinct().OrderBy(x => x).ToListAsync();

                // A Town that repeats its own município's name is not a real freguesia - it is
                // what the geocoder wrote down when it could only place a listing inside the
                // município, not inside one of its actual freguesias ("Loulé" duplicating município
                // Loulé, when the city is really split across São Clemente and São Sebastião).
                // Only dropped when a real freguesia is on offer instead - if it were the only Town
                // on record we would have nothing better to show.
                var realTowns = towns.Where(t => !string.Equals(t, municipality, StringComparison.OrdinalIgnoreCase)).ToList();

                return realTowns.Count > 0 ? realTowns : towns;
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
