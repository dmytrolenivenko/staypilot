
using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Helpers.Calculators;
using StayPilot.Application.Helpers.Mappers;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Application.Interfaces.Services;
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Services
{
    public class OwnedPropertyService : IOwnedPropertyService
    {
        private readonly IOwnedPropertyRepository _ownedPropertyRepository;
        private readonly IMarketAreaRepository _marketAreaRepository;
        private readonly IBeachMarkerRepository _beachMarkerRepository;
        private readonly IPropertyListingRepository _propertyListingRepository;
        private readonly IPremiumFeatureRepository _premiumFeatureRepository;

        // The stored PremiumFeature rows are back: the breakdown quotes the percentages already
        // measured, so a second bathroom cannot read as +4% on the Feature Impact screen and
        // -13% here. The model still prices the property; it just no longer also says what the
        // features are worth. The cost is that a valuation reflects the last recalculation rather
        // than a fresh one - which is the trade we want, because two different answers for the
        // same feature is worse than one answer that is a recalculation behind.
        public OwnedPropertyService(IOwnedPropertyRepository ownedPropertyRepository, IMarketAreaRepository marketAreaRepository, IBeachMarkerRepository beachMarkerRepository, IPropertyListingRepository propertyListingRepository, IPremiumFeatureRepository premiumFeatureRepository)
        {
            _ownedPropertyRepository = ownedPropertyRepository;
            _marketAreaRepository = marketAreaRepository;
            _beachMarkerRepository = beachMarkerRepository;
            _propertyListingRepository = propertyListingRepository;
            _premiumFeatureRepository = premiumFeatureRepository;
        }

        public async Task<OwnedPropertyResponse> AddOwnedPropertyAsync(OwnedPropertyRequest request)
        {
            var marketAreaRepo = await _marketAreaRepository.GetAllMarketAreasAsync();
            var beackMarkerRepo = await _beachMarkerRepository.GetAllBeachMarkersAsync();

            var ownedPropertyEntity = Converter.MapToEntity(request);

            ownedPropertyEntity.MarketAreaId = Calculator.GetMarketId(marketAreaRepo, request.Country, request.District, request.Municipality, request.Town, request.Zone);

            ownedPropertyEntity.MarketArea = marketAreaRepo.FirstOrDefault(x => x.Id == ownedPropertyEntity.MarketAreaId) ?? throw new InvalidOperationException("MarketArea can not be null");

            var closestBeach = Calculator.GetTheClosestBeach(beackMarkerRepo, request.Latitude, request.Longitude);

            // We only fill in beach info when we actually found one. A property with no
            // coordinates just gets null beach fields, same as PropertyListing - that is a
            // normal case, not an error. The lat/lon checks also let the compiler see the
            // .Value reads below are safe.
            if (closestBeach is not null && request.Latitude is not null && request.Longitude is not null)
            {
                ownedPropertyEntity.NearestBeachMarker = closestBeach;
                ownedPropertyEntity.NearestBeachName = closestBeach.Name;

                ownedPropertyEntity.DistanceToBeachMeters = (int)Math.Round(Calculator.CalculateDistanceMeters(
                    (double)closestBeach.Latitude, (double)closestBeach.Longitude,
                    (double)request.Latitude.Value, (double)request.Longitude.Value));
            }

            await _ownedPropertyRepository.CreateOwnedPropertyAsync(ownedPropertyEntity);
            await _ownedPropertyRepository.SaveChangesAsync();

            return Converter.MapToResponse(ownedPropertyEntity);
        }

        public async Task<OwnedPropertyResponse?> GetOwnedPropertyAsync(int id)
        {
            // Now it just reads the row by Id, and returns null when there is no such row.
            var entity = await _ownedPropertyRepository.GetOwnedPropertyAsync(id);

            return entity is null ? null : Converter.MapToResponse(entity);
        }

        public async Task<string?> DeleteOwnedPropertyAsync(int id)
        {
            // Fix: this used to take a string Id and always throw NotImplementedException.
            // The repository already checks if the row exists (it returns null if not).
            var deletedName = await _ownedPropertyRepository.DeleteOwnedPropertyAsync(id);

            if (deletedName is null)
                return null;

            // Fix: Remove() (inside the repository) only stages the delete.
            // We still need to save, or nothing happens in the database.
            await _ownedPropertyRepository.SaveChangesAsync();

            return deletedName;
        }

        public async Task<OwnedPropertyResponse?> UpdateOwnedPropertyAsync(int id, OwnedPropertyRequest request)
        {
            // Fix: this used to build a brand new entity from the request, so any
            // field the caller did not send would overwrite the saved value with
            // blank/default. Now we load the real row and only change what was sent.
            var entity = await _ownedPropertyRepository.GetOwnedPropertyAsync(id);

            if (entity is null)
                return null;

            Converter.ApplyUpdates(entity, request);

            var marketAreaRepo = await _marketAreaRepository.GetAllMarketAreasAsync();

            // Same as Add: the address parts decide the market area. Without this, an edit
            // kept whatever location the property was created with, however the user
            // changed the District/Municipality/Town/Zone pickers.
            entity.MarketAreaId = Calculator.GetMarketId(marketAreaRepo, request.Country, request.District, request.Municipality, request.Town, request.Zone);

            var beachMarkerRepo = await _beachMarkerRepository.GetAllBeachMarkersAsync();

            // Read the coordinates off the entity, not the request: an update that leaves
            // them out still recomputes the beach from the ones already saved.
            var closestBeach = Calculator.GetTheClosestBeach(beachMarkerRepo, entity.Latitude, entity.Longitude);

            if (closestBeach is not null && entity.Latitude is not null && entity.Longitude is not null)
            {
                entity.NearestBeachMarker = closestBeach;
                entity.NearestBeachName = closestBeach.Name;

                entity.DistanceToBeachMeters = (int)Math.Round(Calculator.CalculateDistanceMeters(
                    (double)closestBeach.Latitude, (double)closestBeach.Longitude,
                    (double)entity.Latitude.Value, (double)entity.Longitude.Value));
            }

            entity.UpdatedAtUtc = DateTime.UtcNow;

            await _ownedPropertyRepository.SaveChangesAsync();

            return Converter.MapToResponse(entity);
        }

        public async Task<OwnedPropertyAnalysisResponse?> EstimateOwnedPropertyValue(int id, int radiusMeters, int months)
        {

            var ownedPropertyRepo = await _ownedPropertyRepository.GetOwnedPropertyAsync(id);

            if (ownedPropertyRepo is null)
            {
                return null;
            }

            var ownedProperty = Converter.MapToResponse(ownedPropertyRepo);

            var similarPropertiesRepo = await _propertyListingRepository.GetComparablePropertyListingAsync(ownedProperty.MarketAreaId, ownedProperty.PropertyType,  ownedProperty.Typology, ownedProperty.AreaM2, ownedProperty.Latitude, ownedProperty.Longitude, radiusMeters, months);

            // No comparable listings -> we cannot estimate anything.
            if (similarPropertiesRepo.Count == 0)
            {
                return new OwnedPropertyAnalysisResponse
                {
                    CompsCount = 0,
                    ConfidenceLevel = ValuationConfidence.Low
                };
            }

            // The breakdown is priced from the last recalculation, not from the model that is
            // about to price the property. Empty until ReCalculatePremiumFeaturesValue has run
            // once, which is the same state the Feature Impact screen shows.
            var premiumFeatureRepo = await _premiumFeatureRepository.GetAllPremiumFeaturesAsync();

            var featureEffects = premiumFeatureRepo.Select(x => Converter.MapToFeatureEffect(x)).ToList();

            // Headline price from the fitted model, not the median comp: it holds size,
            // typology, condition and location still, then corrects for the neighbourhood.
            // One call does the lot - price, range, confidence, equity and the breakdown.
            var allListings = await _propertyListingRepository.GetAllListingsForFeaturePremiumCalculationAsync();

            var estimate = PropertyValuation.Fit(allListings).Estimate(ownedProperty, featureEffects);

            // What the raw neighbours ask, unadjusted - shown beside the model's answer.
            var sortedCompPricesPerM2 = similarPropertiesRepo
                .Select(x => x.ListingSnapshots.First().PricePerM2)
                .OrderBy(x => x)
                .ToList();

            var medianCompPricePerM2 = Calculator.Median(sortedCompPricesPerM2);
            var averageCompPricePerM2 = sortedCompPricesPerM2.Average();

            return new OwnedPropertyAnalysisResponse
            {
                MinPrice = estimate.MinPrice,
                MidPrice = estimate.MidPrice,
                MaxPrice = estimate.MaxPrice,
                AveragePrice = averageCompPricePerM2 * ownedProperty.AreaM2,

                ConfidenceLevel = estimate.Confidence,
                CompsCount = similarPropertiesRepo.Count,

                MarketRatePerM2 = medianCompPricePerM2,
                EstimateBeforeAdjustments = medianCompPricePerM2 * ownedProperty.AreaM2,

                MinCompPricePerM2 = Calculator.Percentile(sortedCompPricesPerM2, 0.25),
                MedianCompPricePerM2 = medianCompPricePerM2,
                MaxCompPricePerM2 = Calculator.Percentile(sortedCompPricesPerM2, 0.75),
                AverageCompPricePerM2 = averageCompPricePerM2,

                Adjustments = estimate.Adjustments,

                Comps = similarPropertiesRepo.Select(Converter.MapToComp).ToList(),

                Equity = estimate.Equity,
            };
        }

        public async Task<List<OwnedPropertyResponse>> GetAllOwnedPropertiesAsync()
        {
            var domainOwnedProperties = await _ownedPropertyRepository.GetAllOwnedPropertyAsync();

            // Reuse the same mapper every other method here uses
            return domainOwnedProperties
                .Select(x => Converter.MapToResponse(x))
                .ToList();
        }

    }
}
