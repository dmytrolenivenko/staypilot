using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Contracts.Response.Base;
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

        /// <inheritdoc/>
        public async Task<PremiumFeatureListResponse> GetAllPremiumFeatures()
        {
            var premiumFeatures = await _premiumFeatureRepo.GetAllPremiumFeaturesAsync();

            return new PremiumFeatureListResponse
            {
                Items = premiumFeatures.Select(x => Converter.MapToResponse(x)).ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<PremiumFeatureListResponse> ReCalculatePremiumFeaturesValue()
        {
            var response = new PremiumFeatureListResponse();

            var allListings = await _propertyListingRepo.GetAllListingsForFeaturePremiumCalculationAsync();

            // One regression reads every feature at once, holding size, typology, condition
            // and location still - so a pool premium isn't really "pools come on bigger flats".
            var calculator = FeaturePremiumCalculator.TryFit(allListings, out var usableListings);

            // Too little data to measure anything. That is an answer for the caller, not a crash,
            // and the stored values are left exactly as they were.
            if (calculator is null)
            {
                response.AddError(ErrorCode.NotEnoughListingsToFitModel, usableListings.ToString(), FeaturePremiumCalculator.MinimumListings.ToString());

                return response;
            }

            // Overwrite, not append: exactly one current row per feature.
            var previous = await _premiumFeatureRepo.GetAllPremiumFeaturesAsync();
            _premiumFeatureRepo.RemovePremiumFeatures(previous);

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

                await _premiumFeatureRepo.AddPremiumFeatureAsync(recalculatedFeature);

                response.Items.Add(Converter.MapToResponse(recalculatedFeature));
            }

            await _premiumFeatureRepo.SaveChangesAsync();

            return response;
        }
    }
}
