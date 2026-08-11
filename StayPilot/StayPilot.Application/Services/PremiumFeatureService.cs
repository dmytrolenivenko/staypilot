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

        public async Task<PremiumFeatureResponse> AddPremiumFeatureAsync(PremiumFeature premiumFeature)
        {
            var added = await _premiumFeatureRepo.AddPremiumFeatureAsync(premiumFeature);

            await _premiumFeatureRepo.SaveChangesAsync();

            // Both here and below: map through Converter rather than copying fields by hand, so
            // the confidence range cannot go missing from one path and not the other.
            return Converter.MapToResponse(added);
        }

        public async Task<List<PremiumFeatureResponse>> GetAllPremiumFeatures()
        {
            var premiumFeatures = await _premiumFeatureRepo.GetAllPremiumFeaturesAsync();

            return premiumFeatures.Select(x => Converter.MapToResponse(x)).ToList();
        }

        public async Task<List<PremiumFeature>> ReCalculatePremiumFeaturesValue()
        {
            var allListings = await _propertyListingRepo.GetAllListingsForFeaturePremiumCalculationAsync();

            // Each premium now comes out of the valuation regression rather than being measured
            // on its own. That matters: the regression holds size, typology, condition, market
            // area and beach distance still while it reads one feature, so a pool premium is no
            // longer contaminated by pools coming attached to bigger flats. It also reports a
            // confidence range, which is the only way to tell "worth nothing" apart from
            // "we cannot tell".
            var model = ValuationModel.Fit(allListings);

            // Overwrite, not append: clear the previous results first so the table always
            // ends with exactly one up-to-date row per feature. (Older runs stacked a new
            // row every time, which is why the same feature showed many timestamps.)
            var previous = await _premiumFeatureRepo.GetAllPremiumFeaturesAsync();
            _premiumFeatureRepo.RemovePremiumFeatures(previous);

            var allFeatures = new List<PremiumFeature>();

            foreach (var effect in model.FeatureEffects)
            {
                var recalculatedFeature = new PremiumFeature
                {
                    Feature = effect.Feature,
                    PremiumPercent = effect.Percent,
                    LowerBoundPercent = effect.LowerPercent,
                    UpperBoundPercent = effect.UpperPercent,
                    SampleSize = model.TrainingListings,
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
