
using Microsoft.EntityFrameworkCore;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Domain.Entities;
using StayPilot.Infrastructure.Persistence;

namespace StayPilot.Infrastructure.Repositories
{
    public class PremiumFeatureRepository : IPremiumFeatureRepository
    {
        private readonly StayPilotDbContext _context;

        public PremiumFeatureRepository(StayPilotDbContext context)
        {
            _context = context;
        }

        public async Task<List<PremiumFeature>> GetAllPremiumFeaturesAsync()
        {
            return await _context.PremiumFeatures.ToListAsync();
        }

        public async Task<PremiumFeature> AddPremiumFeatureAsync(PremiumFeature premiumFeature)
        {
            var entry = await _context.PremiumFeatures.AddAsync(premiumFeature);

            return entry.Entity;
        }

        public void RemovePremiumFeatures(IEnumerable<PremiumFeature> premiumFeatures)
        {
            _context.PremiumFeatures.RemoveRange(premiumFeatures);
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
