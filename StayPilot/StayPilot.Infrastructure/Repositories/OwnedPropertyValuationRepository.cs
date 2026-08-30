
using Microsoft.EntityFrameworkCore;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Domain.Entities;
using StayPilot.Infrastructure.Persistence;

namespace StayPilot.Infrastructure.Repositories
{
    public class OwnedPropertyValuationRepository : IOwnedPropertyValuationRepository
    {
        private readonly StayPilotDbContext _context;

        public OwnedPropertyValuationRepository(StayPilotDbContext context)
        {
            _context = context;
        }

        public async Task<List<OwnedPropertyValuation>> GetAllAsync()
        {
            return await _context.OwnedPropertyValuations.ToListAsync();
        }

        public async Task<OwnedPropertyValuation?> GetByOwnedPropertyIdAsync(int ownedPropertyId)
        {
            return await _context.OwnedPropertyValuations
                .FirstOrDefaultAsync(x => x.OwnedPropertyId == ownedPropertyId);
        }

        public async Task AddAsync(OwnedPropertyValuation valuation)
        {
            await _context.OwnedPropertyValuations.AddAsync(valuation);
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
