
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Domain.Entities;
using StayPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace StayPilot.Infrastructure.Repositories
{
    /// <summary>
    /// Talks to the database for beaches.
    /// </summary>
    public class BeachMarkerRepository : IBeachMarkerRepository
    {
        private readonly StayPilotDbContext _context;

        public BeachMarkerRepository(StayPilotDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Reads all beaches from the database.
        /// </summary>
        public async Task<List<BeachMarker>> GetAllBeachMarkersAsync()
        {
            return await _context.BeachMarkers.ToListAsync();
        }
    }
}
