
using StayPilot.Domain.Entities;

namespace StayPilot.Application.Interfaces.Repositories
{
    public interface IOwnedPropertyValuationRepository
    {
        Task<List<OwnedPropertyValuation>> GetAllAsync();

        Task<OwnedPropertyValuation?> GetByOwnedPropertyIdAsync(int ownedPropertyId);

        Task AddAsync(OwnedPropertyValuation valuation);

        Task SaveChangesAsync();
    }
}
