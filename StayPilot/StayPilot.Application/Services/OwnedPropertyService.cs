
using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Contracts.Response.Base;
using StayPilot.Application.Helpers.Calculators;
using StayPilot.Application.Helpers.Mappers;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Application.Interfaces.Services;
using StayPilot.Domain.Entities;
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

            // The breakdown is priced from the last recalculation, not from the model that is
            // about to price the property. Empty until ReCalculatePremiumFeaturesValue has run
            // once, which is the same state the Feature Impact screen shows.
            var premiumFeatureRepo = await _premiumFeatureRepository.GetAllPremiumFeaturesAsync();

            var featureEffects = premiumFeatureRepo.Select(x => Converter.MapToFeatureEffect(x)).ToList();

            // Headline price from the fitted model, not the median comp: it holds size,
            // typology, condition and location still, then corrects for the neighbourhood.
            // One call does the lot - price, range, confidence, equity and the breakdown.
            var allListings = await _propertyListingRepository.GetAllListingsForFeaturePremiumCalculationAsync();

            // Fitted before the comps are fetched, because the fit is what knows where the
            // property is: the comps have to come from the area the coordinates point at, not
            // the one the address happened to name.
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

            var comparableListings = await _propertyListingRepository.GetComparablePropertyListingAsync(estimate.LocatedMarketAreaId, ownedProperty.PropertyType,  ownedProperty.Typology, ownedProperty.AreaM2, ownedProperty.DistanceToBeachMeters, ownedProperty.Latitude, ownedProperty.Longitude, radiusMeters, months);

            // Held to the same standard as the listings the model learned from: broken rows out,
            // re-advertisements of one flat counted once. Without this the comp median is a
            // different market from the estimate printed beside it.
            var comps = ListingQuality.DistinctProperties(
                comparableListings.Where(x => ListingQuality.IsUsable(x, ListingQuality.NewestSnapshot(x))));

            // No comparable listings -> the model still priced it, but there is nothing to show
            // it against, so say that rather than quoting comp statistics over an empty set.
            if (comps.Count == 0)
            {
                return new OwnedPropertyAnalysisResponse
                {
                    CompsCount = 0,
                    ConfidenceLevel = ValuationConfidence.Low
                };
            }

            // Only the nearest handful actually get a say. Distance weighting alone was not enough:
            // the kernel still gives a comparable 800m away most of a vote, so three hundred of
            // them drown out the seventeen next door - which is how "comps alone" read EUR 460,000
            // for a flat whose immediate neighbours ask EUR 321,000. The model has always taken its
            // ten nearest and weighted those; this is the same rule, applied to what we show.
            const int comparablesUsedForStatistics = 25;

            var nearestComps = OrderedByDistanceFrom(ownedProperty, comps)
                .Take(comparablesUsedForStatistics)
                .ToList();

            // What the raw neighbours ask, unadjusted - shown beside the model's answer.
            var sortedCompPricesPerM2 = nearestComps
                .Select(x => ListingQuality.NewestSnapshot(x)!.PricePerM2)
                .OrderBy(x => x)
                .ToList();

            // Weighted by how close each comparable actually is, on the same scale the model uses
            // for its own neighbours. An unweighted average over the whole radius is what made
            // "comps alone" read EUR 464,000 for a flat the nearest seventeen comparables put at
            // EUR 321,000: in a beach town a 2km circle holds two markets, and the dearer one has
            // more listings in it. Falls back to the plain figures when the property has no
            // coordinates, where there is no distance to weight by.
            var weightedComps = ownedProperty.Latitude is null || ownedProperty.Longitude is null
                ? nearestComps.Select(x => (Value: ListingQuality.NewestSnapshot(x)!.PricePerM2, Weight: 1d)).ToList()
                : nearestComps.Select(x => (
                        Value: ListingQuality.NewestSnapshot(x)!.PricePerM2,
                        Weight: x.Latitude is null || x.Longitude is null
                            ? 0d
                            : PropertyValuation.EvidenceWeightAtMeters(Calculator.CalculateDistanceMeters(
                                (double)ownedProperty.Latitude.Value, (double)ownedProperty.Longitude.Value,
                                (double)x.Latitude.Value, (double)x.Longitude.Value))))
                    .ToList();

            var medianCompPricePerM2 = Calculator.WeightedMedian(weightedComps);
            var averageCompPricePerM2 = Calculator.WeightedAverage(weightedComps);

            var marketAreas = await _marketAreaRepository.GetAllMarketAreasAsync();

            var locatedArea = marketAreas.FirstOrDefault(x => x.Id == estimate.LocatedMarketAreaId);

            return new OwnedPropertyAnalysisResponse
            {
                MinPrice = estimate.MinPrice,
                MidPrice = estimate.MidPrice,
                MaxPrice = estimate.MaxPrice,
                AveragePrice = averageCompPricePerM2 * ownedProperty.AreaM2,

                ConfidenceLevel = ConfidenceAfterCrossCheck(estimate, medianCompPricePerM2 * ownedProperty.AreaM2),

                // How many actually back the numbers, not how many the search turned up. Quoting
                // the wider figure made a statistic drawn from twenty-five look like it rested on
                // three hundred.
                CompsCount = nearestComps.Count,
                ComparablesFound = comps.Count,

                MarketRatePerM2 = medianCompPricePerM2,
                EstimateBeforeAdjustments = medianCompPricePerM2 * ownedProperty.AreaM2,

                // The spread stays unweighted on purpose: it answers "what is the range of asking
                // prices around here", which every comparable has an equal say in.
                MinCompPricePerM2 = Calculator.Percentile(sortedCompPricesPerM2, 0.25),
                MedianCompPricePerM2 = Calculator.Median(sortedCompPricesPerM2),
                MaxCompPricePerM2 = Calculator.Percentile(sortedCompPricesPerM2, 0.75),
                AverageCompPricePerM2 = averageCompPricePerM2,

                Adjustments = estimate.Adjustments,

                // Exactly the comparables the statistics came from, so the table can be checked
                // against them rather than being a different sample that happens to sit below.
                Comps = nearestComps.Select(Converter.MapToComp).ToList(),

                LocatedMarketAreaId = estimate.LocatedMarketAreaId,
                LocatedAreaName = locatedArea is null
                    ? string.Empty
                    : string.IsNullOrWhiteSpace(locatedArea.Zone) ? locatedArea.Town : locatedArea.Zone,
                LocatedByCoordinates = estimate.LocatedByCoordinates,

                Equity = estimate.Equity,
            };
        }

        /// <summary>
        /// The comparables, nearest first. A property with no coordinates has no distances to sort
        /// by, so it keeps the order the repository chose - its own market area, closest in size.
        /// </summary>
        private static IEnumerable<PropertyListing> OrderedByDistanceFrom(
            OwnedPropertyResponse property, List<PropertyListing> comps)
        {
            if (property.Latitude is null || property.Longitude is null)
                return comps;

            return comps
                .Where(x => x.Latitude is not null && x.Longitude is not null)
                .OrderBy(x => Calculator.CalculateDistanceMeters(
                    (double)property.Latitude.Value, (double)property.Longitude.Value,
                    (double)x.Latitude!.Value, (double)x.Longitude!.Value))
                .Concat(comps.Where(x => x.Latitude is null || x.Longitude is null));
        }

        /// <summary>
        /// The model's confidence, knocked down a step when the comparables do not agree with it.
        ///
        /// Two ways of reading the same adverts landing far apart is the plainest evidence we have
        /// that the answer is uncertain, and it used to be the one thing confidence ignored: a flat
        /// came back High while the model said EUR 224,000 and the comps said EUR 305,000. "Far
        /// apart" is measured against the range the estimate itself quotes - inside it the two are
        /// telling the same story, outside it they are not.
        /// </summary>
        private static ValuationConfidence ConfidenceAfterCrossCheck(
            PropertyEstimate estimate, decimal compBasedEstimate)
        {
            if (estimate.MidPrice <= 0 || compBasedEstimate <= 0)
                return estimate.Confidence;

            // Literally inside the range we quoted, not merely within its width - the second test
            // is twice as forgiving, and it let a flat keep High confidence while the two methods
            // sat 38% apart.
            if (compBasedEstimate >= estimate.MinPrice && compBasedEstimate <= estimate.MaxPrice)
                return estimate.Confidence;

            return estimate.Confidence switch
            {
                ValuationConfidence.High => ValuationConfidence.Medium,
                ValuationConfidence.Medium => ValuationConfidence.Low,
                _ => ValuationConfidence.Low,
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
