
using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Contracts.Response.Base;
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

            var marketAreaId = Calculator.GetMarketId(marketAreaRepo, request.Country, request.District, request.Municipality, request.Town, request.Zone);

            // No market area for this address -> we cannot place the property, so we save nothing.
            if (marketAreaId is null)
            {
                var noMarketArea = new OwnedPropertyResponse();
                noMarketArea.AddError(ErrorCode.MarketAreaNotFound, Calculator.DescribeAddress(request.Country, request.District, request.Municipality, request.Town, request.Zone));

                return noMarketArea;
            }

            // GetMarketId only ever gives back an Id from the list we just passed it.
            ownedPropertyEntity.MarketAreaId = marketAreaId.Value;
            ownedPropertyEntity.MarketArea = marketAreaRepo.First(x => x.Id == marketAreaId.Value);

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

        public async Task<OwnedPropertyResponse> GetOwnedPropertyAsync(int id)
        {
            // Reads the row by Id. There may not be one.
            var entity = await _ownedPropertyRepository.GetOwnedPropertyAsync(id);

            if (entity is null)
            {
                var notFound = new OwnedPropertyResponse();
                notFound.AddError(ErrorCode.OwnedPropertyNotFound, id.ToString());

                return notFound;
            }

            return Converter.MapToResponse(entity);
        }

        public async Task<DeleteOwnedPropertyResponse> DeleteOwnedPropertyAsync(int id)
        {
            var response = new DeleteOwnedPropertyResponse { Id = id };

            // The repository already checks if the row exists (it returns null if not).
            var deletedName = await _ownedPropertyRepository.DeleteOwnedPropertyAsync(id);

            if (deletedName is null)
            {
                response.AddError(ErrorCode.OwnedPropertyNotFound, id.ToString());

                return response;
            }

            // Fix: Remove() (inside the repository) only stages the delete.
            // We still need to save, or nothing happens in the database.
            await _ownedPropertyRepository.SaveChangesAsync();

            response.Name = deletedName;

            return response;
        }

        public async Task<OwnedPropertyResponse> UpdateOwnedPropertyAsync(int id, OwnedPropertyRequest request)
        {
            // Fix: this used to build a brand new entity from the request, so any
            // field the caller did not send would overwrite the saved value with
            // blank/default. Now we load the real row and only change what was sent.
            var entity = await _ownedPropertyRepository.GetOwnedPropertyAsync(id);

            if (entity is null)
            {
                var notFound = new OwnedPropertyResponse();
                notFound.AddError(ErrorCode.OwnedPropertyNotFound, id.ToString());

                return notFound;
            }

            var marketAreaRepo = await _marketAreaRepository.GetAllMarketAreasAsync();

            // Same as Add: the address parts decide the market area. Without this, an edit
            // kept whatever location the property was created with, however the user
            // changed the District/Municipality/Town/Zone pickers.
            var marketAreaId = Calculator.GetMarketId(marketAreaRepo, request.Country, request.District, request.Municipality, request.Town, request.Zone);

            // Asked before anything is copied onto the entity, so a bad address changes nothing.
            if (marketAreaId is null)
            {
                var noMarketArea = new OwnedPropertyResponse();
                noMarketArea.AddError(ErrorCode.MarketAreaNotFound, Calculator.DescribeAddress(request.Country, request.District, request.Municipality, request.Town, request.Zone));

                return noMarketArea;
            }

            Converter.ApplyUpdates(entity, request);

            entity.MarketAreaId = marketAreaId.Value;

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

        public async Task<OwnedPropertyAnalysisResponse> EstimateOwnedPropertyValue(int id, int radiusMeters, int months)
        {

            var ownedPropertyRepo = await _ownedPropertyRepository.GetOwnedPropertyAsync(id);

            if (ownedPropertyRepo is null)
            {
                var notFound = new OwnedPropertyAnalysisResponse();
                notFound.AddError(ErrorCode.OwnedPropertyNotFound, id.ToString());

                return notFound;
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

            var valuation = PropertyValuation.TryFit(allListings, out var usableListings);

            // Not enough listings in the database to fit a model. Say so rather than crash - the
            // property is fine, we just have nothing to price it against yet.
            if (valuation is null)
            {
                var notEnoughData = new OwnedPropertyAnalysisResponse();
                notEnoughData.AddError(ErrorCode.NotEnoughListingsToFitModel, usableListings.ToString(), PropertyValuation.MinimumListings.ToString());

                return notEnoughData;
            }

            var estimate = valuation.Estimate(ownedProperty, featureEffects);

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

        public async Task<OwnedPropertyListResponse> GetAllOwnedPropertiesAsync()
        {
            var domainOwnedProperties = await _ownedPropertyRepository.GetAllOwnedPropertyAsync();

            // Reuse the same mapper every other method here uses
            return new OwnedPropertyListResponse
            {
                Items = domainOwnedProperties
                    .Select(x => Converter.MapToResponse(x))
                    .ToList()
            };
        }

    }
}
