
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Domain.Entities;
using StayPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace StayPilot.Infrastructure.Repositories
{
    public class BeachMarkerRepository : IBeachMarkerRepository
    {
        private readonly StayPilotDbContext _context;

        public BeachMarkerRepository(StayPilotDbContext context)
        {
            _context = context;
        }

        public async Task<List<BeachMarker>> GetAllBeachMarkersAsync()
        {
            return await _context.BeachMarkers.ToListAsync();
        }
    }
}
