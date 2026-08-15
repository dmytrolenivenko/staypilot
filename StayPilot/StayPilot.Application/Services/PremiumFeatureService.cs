using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Application.Interfaces.Services;
using StayPilot.Domain.Entities;
using StayPilot.Application.Helpers.Calculators;
using StayPilot.Application.Helpers.Mappers;

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

        public async Task<List<PremiumFeatureResponse>> GetAllPremiumFeatures()
        {
            var premiumFeatures = await _premiumFeatureRepo.GetAllPremiumFeaturesAsync();

            return premiumFeatures.Select(x => Converter.MapToResponse(x)).ToList();
        }

        public async Task<List<PremiumFeature>> ReCalculatePremiumFeaturesValue()
        {
            var allListings = await _propertyListingRepo.GetAllListingsForFeaturePremiumCalculationAsync();

            // Every feature is measured by comparing listings in the SAME market area that are
            // alike in every other way and differ only in that feature. The regression this
            // replaced had to hold everything still from one set of coefficients, which is how
            // it ended up reporting a balcony as making a flat cheaper.
            var calculator = FeaturePremiumCalculator.Fit(allListings);

            // Overwrite, not append: exactly one current row per feature.
            var previous = await _premiumFeatureRepo.GetAllPremiumFeaturesAsync();
            _premiumFeatureRepo.RemovePremiumFeatures(previous);

            var allFeatures = new List<PremiumFeature>();

            foreach (var effect in calculator.FeatureEffects)
            {
                var recalculatedFeature = new PremiumFeature
                {
                    Feature = effect.Feature,
                    PremiumPercent = effect.Percent,
                    LowerBoundPercent = effect.LowerPercent,
                    UpperBoundPercent = effect.UpperPercent,
                    SampleSize = calculator.TrainingListings,
                    ListingsWithFeature = effect.ListingsWithFeature,
                    MaximumPercent = effect.MaximumPercent,
                    MaximumBasis = effect.MaximumBasis,
                    Basis = effect.Basis,
                };

                allFeatures.Add(recalculatedFeature);

                await _premiumFeatureRepo.AddPremiumFeatureAsync(recalculatedFeature);
            }

            await _premiumFeatureRepo.SaveChangesAsync();

            return allFeatures;
        }
    }
}
