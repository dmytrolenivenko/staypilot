using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Domain.Entities;
using StayPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace StayPilot.Infrastructure.Repositories
{
    /// <summary>
    /// Reads the seeded per-district growth assumptions.
    /// </summary>
    public class HousePriceGrowthRepository : IHousePriceGrowthRepository
    {
        private readonly StayPilotDbContext _context;

        public HousePriceGrowthRepository(StayPilotDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<HousePriceGrowth?> GetForDistrictAsync(string district)
        {
            var forDistrict = string.IsNullOrWhiteSpace(district)
                ? null
                : await _context.HousePriceGrowth.FirstOrDefaultAsync(x => x.District == district);

            // An unseeded district is normal - the table names the twenty districts, and a market
            // area could be filed under a spelling none of them use. The national row answers for
            // it rather than the forecast going silent.
            return forDistrict ?? await _context.HousePriceGrowth.FirstOrDefaultAsync(x => x.District == string.Empty);
        }

        /// <inheritdoc/>
        public async Task<List<HousePriceGrowth>> GetAllAsync()
        {
            return await _context.HousePriceGrowth
                .OrderBy(x => x.District)
                .ToListAsync();
        }
    }
}
