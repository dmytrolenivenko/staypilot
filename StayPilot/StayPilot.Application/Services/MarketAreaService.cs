using Microsoft.EntityFrameworkCore;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Interfaces;
using StayPilot.Infrastructure.Persistence;

namespace StayPilot.Application.Services
{
    public class MarketAreaService : IMarketAreaService
    {
        private readonly StayPilotDbContext _context;
        public MarketAreaService(StayPilotDbContext context)
        {
            _context = context;
        }

        public async Task<List<MarketAreaResponse>> GetAllMarketAreasAsync()
        {
            var marketAreas = await _context.MarketAreas.ToListAsync();

            return marketAreas.Select(x => new MarketAreaResponse
            {
                Id = x.Id,
                Country = x.Country,
                District = x.District,
                Municipality = x.Municipality,
                Town = x.Town,
                Zone = x.Zone,
                Notes = x.Notes
            }).ToList();
         }
    }
}
