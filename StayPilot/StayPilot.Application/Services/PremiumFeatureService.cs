using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Application.Interfaces.Services;
using StayPilot.Domain.Entities;
using StayPilot.Application.Helpers.Calculators;

namespace StayPilot.Application.Services
{
    public class PremiumFeatureService : IPremiumFeatureService
    {
        private readonly IPremiumFeatureRepository _premiumFeatureRepo;
        private readonly IPropertyListingRepository _propertyListingRepo;

        public PremiumFeatureService(IPremiumFeatureRepository premiumFeatureRepo, IPropertyListingRepository propertyListingRepo)
        {
            _premiumFeatureRepo = premiumFeatureRepo;
            _propertyListingRepo = propertyListingRepo;
        }

        public async Task<PremiumFeatureResponse> AddPremiumFeatureAsync(PremiumFeature premiumFeature)
        {
            var added = await _premiumFeatureRepo.AddPremiumFeatureAsync(premiumFeature);

            await _premiumFeatureRepo.SaveChangesAsync();
            
            return new PremiumFeatureResponse
            {
                Feature = added.Feature,
                PremiumPercent = added.PremiumPercent,
                CalculatedAtUtc = added.CalculatedAtUtc
            };
        }

        public async Task<List<PremiumFeatureResponse>> GetAllPremiumFeatures()
        {
            var premiumFeatures = await _premiumFeatureRepo.GetAllPremiumFeaturesAsync();

            return premiumFeatures.Select(x => new PremiumFeatureResponse
            {
                Feature = x.Feature,
                PremiumPercent = x.PremiumPercent,
                CalculatedAtUtc = x.CalculatedAtUtc
            }).ToList();
        }

        public async Task<List<PremiumFeature>> ReCalculatePremiumFeaturesValue()
        {
            var allListings = await _propertyListingRepo.GetAllListingsForFeaturePremiumCalculationAsync();

            // Overwrite, not append: clear the previous results first so the table always
            // ends with exactly one up-to-date row per feature. (Older runs stacked a new
            // row every time, which is why the same feature showed many timestamps.)
            var previous = await _premiumFeatureRepo.GetAllPremiumFeaturesAsync();
            _premiumFeatureRepo.RemovePremiumFeatures(previous);

            var allFeatures = new List<PremiumFeature>();

            // Same list the Calculator itself uses internally - so adding or removing
            // a tracked feature only ever means editing TrackedFeatures in Calculator.cs.
            foreach (var feature in Calculator.TrackedFeatureNames)
            {
                var valueInPercent = Calculator.CalculateFeaturePremiumPercent(allListings, feature);

                var recalculatedFeature = new PremiumFeature
                {
                    Feature = feature,
                    PremiumPercent = valueInPercent,
                };

                allFeatures.Add(recalculatedFeature);

                await _premiumFeatureRepo.AddPremiumFeatureAsync(recalculatedFeature);
            }

            await _premiumFeatureRepo.SaveChangesAsync();

            return allFeatures;
        }
    }
}
