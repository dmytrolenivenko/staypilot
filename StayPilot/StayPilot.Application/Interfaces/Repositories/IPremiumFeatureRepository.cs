
using StayPilot.Domain.Entities;

namespace StayPilot.Application.Interfaces.Repositories
{
    public interface IPremiumFeatureRepository
    {
        Task<List<PremiumFeature>> GetAllPremiumFeaturesAsync();

        Task<PremiumFeature> AddPremiumFeatureAsync(PremiumFeature premiumFeature);

        Task SaveChangesAsync();
    }
}
