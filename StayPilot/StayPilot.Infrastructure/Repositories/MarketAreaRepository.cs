using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Domain.Entities;
using StayPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace StayPilot.Infrastructure.Repositories
{
    public class MarketAreaRepository : IMarketAreaRepository
    {
        private readonly StayPilotDbContext _context;

        public MarketAreaRepository(StayPilotDbContext context)
        {
            _context = context;
        }

        public async Task<List<MarketArea>> GetAllMarketAreasAsync()
        {
            return await _context.MarketAreas.ToListAsync();
        }

        public async Task<List<string>> GetMarketAreaOptionsAsync(string? district, string? municipality, string? town)
        {
            var query = _context.MarketAreas.AsQueryable();

            if (town is not null)
            {
                return await query.Where(x => x.Town == town && x.Zone != null).Select(x => x.Zone).Distinct().OrderBy(x => x).ToListAsync();
            }
            else if (municipality is not null)
            {
                return await query.Where(x => x.Municipality == municipality).Select(x => x.Town).Distinct().OrderBy(x => x).ToListAsync();
            }
            else if (district is not null)
            {
                return await query.Where(x => x.District == district).Select(x => x.Municipality).Distinct().OrderBy(x => x).ToListAsync();
            }

            return await _context.MarketAreas.Select(x => x.Municipality).ToListAsync();
        }
    }
}
