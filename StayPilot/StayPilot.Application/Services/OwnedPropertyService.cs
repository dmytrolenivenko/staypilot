
using System.Text.Json;
using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Contracts.Response.Base;
using StayPilot.Application.Contracts.Response.SubResponse;
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
        private readonly IHousePriceGrowthRepository _housePriceGrowthRepository;
        private readonly ICurrentUser _currentUser;

        // The stored PremiumFeature rows are back: the breakdown quotes the percentages already
        // measured, so a second bathroom cannot read as +4% on the Feature Impact screen and
        // -13% here. The model still prices the property; it just no longer also says what the
        // features are worth. The cost is that a valuation reflects the last recalculation rather
        // than a fresh one - which is the trade we want, because two different answers for the
        // same feature is worse than one answer that is a recalculation behind.
        public OwnedPropertyService(
            IOwnedPropertyRepository ownedPropertyRepository, 
            IMarketAreaRepository marketAreaRepository, 
            IBeachMarkerRepository beachMarkerRepository, 
            IPropertyListingRepository propertyListingRepository, 
            IPremiumFeatureRepository premiumFeatureRepository, 
            IHousePriceGrowthRepository housePriceGrowthRepository,
            ICurrentUser currentUser
            )
        {
            _ownedPropertyRepository = ownedPropertyRepository;
            _marketAreaRepository = marketAreaRepository;
            _beachMarkerRepository = beachMarkerRepository;
            _propertyListingRepository = propertyListingRepository;
            _premiumFeatureRepository = premiumFeatureRepository;
            _housePriceGrowthRepository = housePriceGrowthRepository;
            _currentUser = currentUser;
        }

        public async Task<OwnedPropertyResponse> AddOwnedPropertyAsync(OwnedPropertyRequest request)
        {
            var marketAreaRepo = await _marketAreaRepository.GetAllMarketAreasAsync();
            var beackMarkerRepo = await _beachMarkerRepository.GetAllBeachMarkersAsync();

            var ownedPropertyEntity = Converter.MapToEntity(request);
            ownedPropertyEntity.OwnerUserId = await _currentUser.GetCurrentUserIdAsync();

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
            // Getting HttpCaller Id
            var userId = await _currentUser.GetCurrentUserIdAsync();

            // Reads the row by Id. There may not be one.
            var entity = await _ownedPropertyRepository.GetOwnedPropertyAsync(id, userId);

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

            // Getting HttpCaller Id
            var userId = await _currentUser.GetCurrentUserIdAsync();

            // The repository already checks if the row exists (it returns null if not).
            var deletedName = await _ownedPropertyRepository.DeleteOwnedPropertyAsync(id, userId);

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
            // Getting HttpCaller Id
            var userId = await _currentUser.GetCurrentUserIdAsync();

            // Fix: this used to build a brand new entity from the request, so any
            // field the caller did not send would overwrite the saved value with
            // blank/default. Now we load the real row and only change what was sent.
            var entity = await _ownedPropertyRepository.GetOwnedPropertyAsync(id, userId);

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
            // Getting HttpCaller Id
            var userId = await _currentUser.GetCurrentUserIdAsync();

            var ownedPropertyRepo = await _ownedPropertyRepository.GetOwnedPropertyAsync(id, userId);

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

            var (nearestComps, medianCompPricePerM2, averageCompPricePerM2) = NearestCompStatistics(ownedProperty, comps);

            // What the raw neighbours ask, unadjusted - shown beside the model's answer.
            var sortedCompPricesPerM2 = nearestComps
                .Select(x => ListingQuality.NewestSnapshot(x)!.PricePerM2)
                .OrderBy(x => x)
                .ToList();

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
                CompPricePerM2P25 = Calculator.Percentile(sortedCompPricesPerM2, 0.25),
                MedianCompPricePerM2 = Calculator.Median(sortedCompPricesPerM2),
                CompPricePerM2P75 = Calculator.Percentile(sortedCompPricesPerM2, 0.75),
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

                AskSpread = estimate.AskSpread,
            };
        }

        /// <inheritdoc/>
        public async Task<OwnedPropertyPortfolioResponse> RevalueOwnedPropertiesAsync(int radiusMeters, int months, int years)
        {
            var asOfUtc = DateTime.UtcNow;

            // Getting HttpCaller Id
            var userId = await _currentUser.GetCurrentUserIdAsync();

            var response = new OwnedPropertyPortfolioResponse
            {
                GeneratedAtUtc = asOfUtc,
                ProjectionYears = years,
            };

            var owned = await _ownedPropertyRepository.GetAllOwnedPropertyAsync(userId);

            // No properties is not an error - it is what the screen shows before you add one.
            if (owned.Count == 0)
            {
                return response;
            }

            var premiumFeatureRepo = await _premiumFeatureRepository.GetAllPremiumFeaturesAsync();

            var featureEffects = premiumFeatureRepo.Select(x => Converter.MapToFeatureEffect(x)).ToList();

            // Fitted once for the whole portfolio. The fit reads every listing in the database, so
            // pricing ten properties through ten calls to the single-property endpoint would read
            // that table ten times to reach the same answer.
            var allListings = await _propertyListingRepository.GetAllListingsForFeaturePremiumCalculationAsync();

            var valuation = PropertyValuation.TryFit(allListings, out var usableListings);

            if (valuation is null)
            {
                response.AddError(ErrorCode.NotEnoughListingsToFitModel, usableListings.ToString(), PropertyValuation.MinimumListings.ToString());

                return response;
            }

            var marketAreas = await _marketAreaRepository.GetAllMarketAreasAsync();

            // Demand and the local trend describe a place, not a property, so two flats in the
            // same município share one answer and one read of that município's history.
            var outlooks = new Dictionary<string, AreaOutlook>(StringComparer.OrdinalIgnoreCase);

            foreach (var entity in owned)
            {
                var item = await BuildPortfolioItemAsync(
                    entity, valuation, featureEffects, marketAreas, outlooks, radiusMeters, months, years, asOfUtc);

                item.ValuatedAtUtc = asOfUtc;

                response.Items.Add(item);

                // Staged only - one SaveChangesAsync below persists every property's row together.
                await _ownedPropertyRepository.UpsertValuationAsync(
                    BuildValuationEntity(entity.Id, item, asOfUtc));
            }

            await _ownedPropertyRepository.SaveChangesAsync();

            // Most valuable first: the list is read to decide what to do next, and the biggest
            // number on the page is where that decision usually starts.
            response.Items = response.Items.OrderByDescending(x => x.MidPrice).ToList();

            response.PropertyCount = response.Items.Count;
            response.TotalEstimatedAskingPrice = response.Items.Sum(x => x.MidPrice);
            response.TotalPurchasePrice = response.Items.Sum(x => x.AskSpread.PurchasePrice);
            response.TotalAskSpreadAmount = response.TotalEstimatedAskingPrice - response.TotalPurchasePrice;

            response.TotalAskSpreadPercent = response.TotalPurchasePrice <= 0m
                ? 0m
                : Math.Round(response.TotalAskSpreadAmount / response.TotalPurchasePrice * 100m, 1);

            // The Base path only. Adding up the optimistic paths would produce a portfolio total
            // that assumes every district has its best decade at once.
            response.TotalProjectedAskingPrice = response.Items.Sum(x =>
                x.Forecast.Scenarios.FirstOrDefault(s => s.Name == BaseScenarioName)?.FinalYearValue ?? x.MidPrice);

            return response;
        }

        /// <inheritdoc/>
        public async Task<OwnedPropertyValuationResponse> RevalueOwnedPropertyAsync(int id, int radiusMeters, int months, int years)
        {
            var response = new OwnedPropertyValuationResponse();

            // Getting HttpCaller Id
            var userId = await _currentUser.GetCurrentUserIdAsync();

            var entity = await _ownedPropertyRepository.GetOwnedPropertyAsync(id, userId);

            if (entity is null)
            {
                response.AddError(ErrorCode.OwnedPropertyNotFound, id.ToString());

                return response;
            }

            var premiumFeatureRepo = await _premiumFeatureRepository.GetAllPremiumFeaturesAsync();

            var featureEffects = premiumFeatureRepo.Select(x => Converter.MapToFeatureEffect(x)).ToList();

            var allListings = await _propertyListingRepository.GetAllListingsForFeaturePremiumCalculationAsync();

            var valuation = PropertyValuation.TryFit(allListings, out var usableListings);

            if (valuation is null)
            {
                response.AddError(ErrorCode.NotEnoughListingsToFitModel, usableListings.ToString(), PropertyValuation.MinimumListings.ToString());

                return response;
            }

            var marketAreas = await _marketAreaRepository.GetAllMarketAreasAsync();
            var outlooks = new Dictionary<string, AreaOutlook>(StringComparer.OrdinalIgnoreCase);
            var asOfUtc = DateTime.UtcNow;

            var item = await BuildPortfolioItemAsync(
                entity, valuation, featureEffects, marketAreas, outlooks, radiusMeters, months, years, asOfUtc);

            item.ValuatedAtUtc = asOfUtc;

            await _ownedPropertyRepository.UpsertValuationAsync(BuildValuationEntity(id, item, asOfUtc));
            await _ownedPropertyRepository.SaveChangesAsync();

            response.Item = item;

            return response;
        }

        /// <summary>The scenario the portfolio totals are added up from.</summary>
        private const string BaseScenarioName = "Base";

        /// <summary>
        /// Prices one owned property and works out what its place is doing around it.
        /// </summary>
        private async Task<OwnedPropertyPortfolioItemResponse> BuildPortfolioItemAsync(
            OwnedProperty entity,
            PropertyValuation valuation,
            IReadOnlyList<FeatureEffect> featureEffects,
            IReadOnlyList<MarketArea> marketAreas,
            Dictionary<string, AreaOutlook> outlooks,
            int radiusMeters,
            int months,
            int years,
            DateTime asOfUtc)
        {
            var ownedProperty = Converter.MapToResponse(entity);

            var estimate = valuation.Estimate(ownedProperty, featureEffects);

            // The zone the model actually priced it as, which the coordinates can override. The
            // stored one is the fallback, because a property with no coordinates still has an
            // address.
            var locatedArea = marketAreas.FirstOrDefault(x => x.Id == estimate.LocatedMarketAreaId);
            var storedArea = marketAreas.FirstOrDefault(x => x.Id == entity.MarketAreaId);

            var placedIn = locatedArea ?? storedArea;

            var item = new OwnedPropertyPortfolioItemResponse
            {
                Id = entity.Id,
                Name = entity.Name,
                PropertyType = entity.PropertyType,
                Typology = entity.Typology,
                AreaM2 = entity.AreaM2,

                District = placedIn?.District ?? string.Empty,
                Municipality = placedIn?.Municipality ?? string.Empty,
                Town = placedIn?.Town ?? string.Empty,

                LocatedAreaName = locatedArea is null
                    ? string.Empty
                    : string.IsNullOrWhiteSpace(locatedArea.Zone) ? locatedArea.Town : locatedArea.Zone,
                LocatedByCoordinates = estimate.LocatedByCoordinates,

                MidPrice = estimate.MidPrice,
                MinPrice = estimate.MinPrice,
                MaxPrice = estimate.MaxPrice,
                PricePerM2 = entity.AreaM2 <= 0 ? 0m : Math.Round(estimate.MidPrice / entity.AreaM2, 0),
                ConfidenceLevel = estimate.Confidence,
                AskSpread = estimate.AskSpread,
            };

            // Cross-checked against what the immediate neighbours ask, exactly as the detail panel
            // does it - otherwise the same property would carry one confidence in the list and a
            // different one when opened.
            var comparableListings = await _propertyListingRepository.GetComparablePropertyListingAsync(
                estimate.LocatedMarketAreaId, ownedProperty.PropertyType, ownedProperty.Typology, ownedProperty.AreaM2,
                ownedProperty.DistanceToBeachMeters, ownedProperty.Latitude, ownedProperty.Longitude, radiusMeters, months);

            var comps = ListingQuality.DistinctProperties(
                comparableListings.Where(x => ListingQuality.IsUsable(x, ListingQuality.NewestSnapshot(x))));

            if (comps.Count == 0)
            {
                // The model still priced it, but with nothing nearby to check the answer against
                // it is the weakest kind of estimate we produce, and says so.
                item.ConfidenceLevel = ValuationConfidence.Low;
                item.ConfidenceNote = "no comparable adverts nearby to check the estimate against";
            }
            else
            {
                var (_, medianCompPricePerM2, _) = NearestCompStatistics(ownedProperty, comps);

                item.ConfidenceLevel = ConfidenceAfterCrossCheck(estimate, medianCompPricePerM2 * ownedProperty.AreaM2);

                if (item.ConfidenceLevel != ValuationConfidence.High)
                {
                    item.ConfidenceNote = $"checked against {comps.Count} nearby adverts, and either they disagree with the model or there are too few close by";
                }
            }

            var outlook = await GetOutlookAsync(item.District, item.Municipality, outlooks, asOfUtc);

            item.Demand = Converter.MapToDemand(outlook.Demand, DescribePlace(item.Municipality, item.District));

            item.Forecast = Converter.MapToForecast(
                GrowthForecastCalculator.Calculate(estimate.MidPrice, outlook.Growth, outlook.Trend, years),
                outlook.Growth.District,
                years);

            return item;
        }

        /// <summary>
        /// The demand score, local price trend and seeded growth rate for one place, read once and
        /// then handed to every property that sits in it.
        ///
        /// Scoped to the município rather than the freguesia: a parish rarely holds the ten
        /// listings the demand score needs, and a whole district is too broad to say anything
        /// about a particular street.
        /// </summary>
        private async Task<AreaOutlook> GetOutlookAsync(
            string district, string municipality, Dictionary<string, AreaOutlook> cache, DateTime asOfUtc)
        {
            var key = $"{district}|{municipality}";

            if (cache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var listings = await _propertyListingRepository.GetListingsWithHistoryAsync(district, municipality, null);

            var growth = await _housePriceGrowthRepository.GetForDistrictAsync(district);

            var outlook = new AreaOutlook(
                DemandCalculator.Calculate(listings, asOfUtc),
                GrowthForecastCalculator.MeasureLocalTrend(listings, asOfUtc),
                // Only when the seed table itself is empty, which means a migration did not run.
                // A flat zero growth is the one assumption that cannot mislead about direction.
                growth ?? new HousePriceGrowth
                {
                    District = district,
                    AnnualGrowthPercent = 0m,
                    VolatilityPercentagePoints = 0m,
                    Source = "No growth assumption is seeded for this district, so nothing is projected.",
                    AsOfYear = asOfUtc.Year,
                });

            cache[key] = outlook;

            return outlook;
        }

        /// <summary>Names a place the way the demand block prints it.</summary>
        private static string DescribePlace(string municipality, string district)
        {
            if (!string.IsNullOrWhiteSpace(municipality) && !string.IsNullOrWhiteSpace(district))
            {
                return $"{municipality}, {district}";
            }

            return string.IsNullOrWhiteSpace(municipality) ? district : municipality;
        }

        /// <summary>What one place is doing, cached for every property inside it.</summary>
        private readonly record struct AreaOutlook(
            DemandCalculator.DemandOutcome Demand,
            GrowthForecastCalculator.LocalTrend Trend,
            HousePriceGrowth Growth);

        /// <summary>
        /// The nearest comparables to one property, and what they ask, weighted by how close each
        /// one actually is.
        ///
        /// Only the nearest handful get a say. Distance weighting alone was not enough: the kernel
        /// still gives a comparable 800m away most of a vote, so three hundred of them drown out
        /// the seventeen next door - which is how "comps alone" read EUR 460,000 for a flat whose
        /// immediate neighbours ask EUR 321,000. The model has always taken its ten nearest and
        /// weighted those; this is the same rule, applied to what we show.
        ///
        /// One method rather than two because the Valuation panel and the portfolio list both
        /// cross-check the model against these numbers, and two copies of the rule would
        /// eventually give the same property two different confidences.
        /// </summary>
        private static (List<PropertyListing> Nearest, decimal WeightedMedianPricePerM2, decimal WeightedAveragePricePerM2)
            NearestCompStatistics(OwnedPropertyResponse ownedProperty, List<PropertyListing> comps)
        {
            const int comparablesUsedForStatistics = 25;

            var nearestComps = OrderedByDistanceFrom(ownedProperty, comps)
                .Take(comparablesUsedForStatistics)
                .ToList();

            // An unweighted average over the whole radius is what made "comps alone" read
            // EUR 464,000 for a flat the nearest seventeen comparables put at EUR 321,000: in a
            // beach town a 2km circle holds two markets, and the dearer one has more listings in
            // it. Falls back to the plain figures when the property has no coordinates, where
            // there is no distance to weight by.
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

            return (nearestComps, Calculator.WeightedMedian(weightedComps), Calculator.WeightedAverage(weightedComps));
        }

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
            // Getting HttpCaller Id
            var userId = await _currentUser.GetCurrentUserIdAsync();

            var domainOwnedProperties = await _ownedPropertyRepository.GetAllOwnedPropertyAsync(userId);
            var valuations = await _ownedPropertyRepository.GetAllValuationsAsync();

            // Reuse the same mapper every other method here uses, then stamp on whether (and at
            // what price) each property was last valued - My Properties reads this to show
            // "not evaluated yet" instead of sending the caller to a second endpoint for it.
            return new OwnedPropertyListResponse
            {
                Items = domainOwnedProperties
                    .Select(x =>
                    {
                        var response = Converter.MapToResponse(x);

                        if (valuations.TryGetValue(x.Id, out var cached))
                        {
                            response.ValuatedMidPrice = DeserializeSnapshot(cached).MidPrice;
                            response.ValuatedAtUtc = cached.ValuatedAtUtc;
                        }

                        return response;
                    })
                    .ToList()
            };
        }

        /// <summary>Reads back what a revaluation cached for one property.</summary>
        private static OwnedPropertyValuationSnapshot DeserializeSnapshot(OwnedPropertyValuation cached)
        {
            return JsonSerializer.Deserialize<OwnedPropertyValuationSnapshot>(cached.ResultJson)
                ?? new OwnedPropertyValuationSnapshot();
        }

        /// <summary>
        /// Reads back the last priced result for every owned property, straight from the cache -
        /// no model fit, no comp search. A property with no cached row yet is priced at zero with
        /// ValuatedAtUtc null, which the screen reads as "not evaluated yet".
        /// </summary>
        /// <inheritdoc/>
        public async Task<OwnedPropertyPortfolioResponse> GetCachedPortfolioAsync()
        {
            // Getting HttpCaller Id
            var userId = await _currentUser.GetCurrentUserIdAsync();

            var owned = await _ownedPropertyRepository.GetAllOwnedPropertyAsync(userId);

            var response = new OwnedPropertyPortfolioResponse();

            if (owned.Count == 0)
            {
                return response;
            }

            var valuations = await _ownedPropertyRepository.GetAllValuationsAsync();
            var marketAreas = await _marketAreaRepository.GetAllMarketAreasAsync();

            foreach (var entity in owned)
            {
                response.Items.Add(BuildCachedPortfolioItem(entity, valuations, marketAreas));
            }

            response.Items = response.Items.OrderByDescending(x => x.MidPrice).ToList();

            response.PropertyCount = response.Items.Count;
            response.TotalEstimatedAskingPrice = response.Items.Sum(x => x.MidPrice);
            response.TotalPurchasePrice = response.Items.Sum(x => x.AskSpread.PurchasePrice);
            response.TotalAskSpreadAmount = response.TotalEstimatedAskingPrice - response.TotalPurchasePrice;

            response.TotalAskSpreadPercent = response.TotalPurchasePrice <= 0m
                ? 0m
                : Math.Round(response.TotalAskSpreadAmount / response.TotalPurchasePrice * 100m, 1);

            response.ProjectionYears = response.Items.Select(x => x.Forecast.Years).DefaultIfEmpty(0).Max();

            response.TotalProjectedAskingPrice = response.Items.Sum(x =>
                x.Forecast.Scenarios.FirstOrDefault(s => s.Name == BaseScenarioName)?.FinalYearValue ?? x.MidPrice);

            // The oldest of the per-property valuation times, not "now" - the cache was not just
            // computed, and this header would otherwise overstate how fresh the numbers are.
            // Null when nothing has ever been valued, rather than a fake 0001-01-01.
            var valuedTimes = response.Items.Where(x => x.ValuatedAtUtc.HasValue).Select(x => x.ValuatedAtUtc!.Value);

            response.GeneratedAtUtc = valuedTimes.Any() ? valuedTimes.Min() : null;

            return response;
        }

        /// <summary>One property's row for the cached-read path: cached numbers if there are any,
        /// a "not evaluated yet" placeholder if not.</summary>
        private static OwnedPropertyPortfolioItemResponse BuildCachedPortfolioItem(
            OwnedProperty entity,
            Dictionary<int, OwnedPropertyValuation> valuations,
            IReadOnlyList<MarketArea> marketAreas)
        {
            var item = new OwnedPropertyPortfolioItemResponse
            {
                Id = entity.Id,
                Name = entity.Name,
                PropertyType = entity.PropertyType,
                Typology = entity.Typology,
                AreaM2 = entity.AreaM2,
            };

            if (!valuations.TryGetValue(entity.Id, out var cached))
            {
                // Never valued: nothing to price it against yet, but it still has an address.
                var storedArea = marketAreas.FirstOrDefault(x => x.Id == entity.MarketAreaId);

                item.District = storedArea?.District ?? string.Empty;
                item.Municipality = storedArea?.Municipality ?? string.Empty;
                item.Town = storedArea?.Town ?? string.Empty;
                item.ConfidenceNote = "Not evaluated yet - press Re-price.";
                item.AskSpread = PropertyValuation.BuildAskSpread(entity.PurchasePrice, entity.PurchaseDate, 0m);

                return item;
            }

            var snapshot = DeserializeSnapshot(cached);

            item.District = snapshot.District;
            item.Municipality = snapshot.Municipality;
            item.Town = snapshot.Town;
            item.LocatedAreaName = snapshot.LocatedAreaName;
            item.LocatedByCoordinates = snapshot.LocatedByCoordinates;
            item.MidPrice = snapshot.MidPrice;
            item.MinPrice = snapshot.MinPrice;
            item.MaxPrice = snapshot.MaxPrice;
            item.PricePerM2 = snapshot.PricePerM2;
            item.ConfidenceLevel = snapshot.ConfidenceLevel;
            item.ConfidenceNote = snapshot.ConfidenceNote;
            item.Demand = snapshot.Demand;
            item.Forecast = snapshot.Forecast;
            item.ValuatedAtUtc = cached.ValuatedAtUtc;

            // Rebuilt live, not cached: purchase price/date can change without a revaluation, and
            // a cached spread would then quietly compare the new price against an old estimate.
            item.AskSpread = PropertyValuation.BuildAskSpread(entity.PurchasePrice, entity.PurchaseDate, snapshot.MidPrice);

            return item;
        }

        /// <summary>Turns one freshly computed row into the entity a revaluation persists.</summary>
        private static OwnedPropertyValuation BuildValuationEntity(
            int ownedPropertyId, OwnedPropertyPortfolioItemResponse item, DateTime asOfUtc)
        {
            var snapshot = new OwnedPropertyValuationSnapshot
            {
                District = item.District,
                Municipality = item.Municipality,
                Town = item.Town,
                LocatedAreaName = item.LocatedAreaName,
                LocatedByCoordinates = item.LocatedByCoordinates,
                MidPrice = item.MidPrice,
                MinPrice = item.MinPrice,
                MaxPrice = item.MaxPrice,
                PricePerM2 = item.PricePerM2,
                ConfidenceLevel = item.ConfidenceLevel,
                ConfidenceNote = item.ConfidenceNote,
                Demand = item.Demand,
                Forecast = item.Forecast,
            };

            return new OwnedPropertyValuation
            {
                OwnedPropertyId = ownedPropertyId,
                ResultJson = JsonSerializer.Serialize(snapshot),
                ValuatedAtUtc = asOfUtc,
            };
        }
    }
}
