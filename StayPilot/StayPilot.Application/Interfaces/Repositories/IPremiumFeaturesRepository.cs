
using StayPilot.Domain.Entities;

namespace StayPilot.Application.Interfaces.Repositories
{
    public interface IPremiumFeaturesRepository
    {
        Task<PremiumFeatures> GetPremiumFeaturesAsync();
    }
}
