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
    }
}
