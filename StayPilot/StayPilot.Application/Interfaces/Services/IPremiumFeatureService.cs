using StayPilot.Application.Contracts.Response;
using StayPilot.Domain.Entities;

namespace StayPilot.Application.Interfaces.Services
{
    public interface IPremiumFeatureService
    {
        Task<PremiumFeatureResponse> AddPremiumFeatureAsync(PremiumFeature premiumFeature);

        Task<List<PremiumFeatureResponse>> GetAllPremiumFeatures();

        Task<List<PremiumFeature>> ReCalculatePremiumFeaturesValue();
    }
}
