
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
    /// <inheritdoc cref="IOwnedPropertyValuationService"/>
    public class OwnedPropertyValuationService : IOwnedPropertyValuationService
    {
        private readonly IOwnedPropertyRepository _ownedPropertyRepository;
        private readonly IMarketAreaRepository _marketAreaRepository;
        private readonly IPropertyListingRepository _propertyListingRepository;
        private readonly IHousePriceGrowthRepository _housePriceGrowthRepository;

        public OwnedPropertyValuationService(
            IOwnedPropertyRepository ownedPropertyRepository,
            IMarketAreaRepository marketAreaRepository,
            IPropertyListingRepository propertyListingRepository,
            IHousePriceGrowthRepository housePriceGrowthRepository)
        {
            _ownedPropertyRepository = ownedPropertyRepository;
            _marketAreaRepository = marketAreaRepository;
            _propertyListingRepository = propertyListingRepository;
            _housePriceGrowthRepository = housePriceGrowthRepository;
        }

        /// <inheritdoc/>
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

            // The property's own stored area, not a coordinate-voted one - simpler, and the
            // comps search below only uses this as an ordering tiebreaker, not a hard filter.
            // One row is all this needs, so a single lookup beats loading every market area.
            var locatedArea = await _marketAreaRepository.GetMarketAreaByIdAsync(ownedProperty.MarketAreaId);
            var locatedAreaName = locatedArea is null
                ? string.Empty
                : string.IsNullOrWhiteSpace(locatedArea.Zone) ? locatedArea.Town : locatedArea.Zone;

            var comps = await GetUsableComparablesAsync(ownedProperty.MarketAreaId, ownedProperty, radiusMeters, months);

            // Nothing nearby to price it against. Rather than invent a number, say so plainly:
            // Low confidence and no comps is the honest answer for a property somewhere thin.
            if (comps.Count == 0)
            {
                return new OwnedPropertyAnalysisResponse
                {
                    ConfidenceLevel = ValuationConfidence.Low,
                    CompsCount = 0,
                    ComparablesFound = 0,

                    LocatedMarketAreaId = ownedProperty.MarketAreaId,
                    LocatedAreaName = locatedAreaName,
                    LocatedByCoordinates = false,

                    AskSpread = BuildAskSpread(ownedProperty.PurchasePrice, ownedProperty.PurchaseDate, estimatedAsk: 0),
                };
            }

            var pricing = PriceFromComps(ownedProperty, comps);

            return new OwnedPropertyAnalysisResponse
            {
                MinPrice = pricing.MinPrice,
                MidPrice = pricing.MidPrice,
                MaxPrice = pricing.MaxPrice,
                AveragePrice = pricing.AveragePrice,

                ConfidenceLevel = pricing.Confidence,

                // How many actually back the numbers, not how many the search turned up.
                CompsCount = pricing.NearestComps.Count,
                ComparablesFound = comps.Count,

                MarketRatePerM2 = pricing.MarketRatePerM2,
                EstimateBeforeAdjustments = pricing.MarketRatePerM2 * ownedProperty.AreaM2,

                CompPricePerM2P25 = pricing.CompPricePerM2P25,
                MedianCompPricePerM2 = pricing.MedianCompPricePerM2,
                CompPricePerM2P75 = pricing.CompPricePerM2P75,
                AverageCompPricePerM2 = pricing.AverageCompPricePerM2,

                Comps = pricing.NearestComps.Select(Converter.MapToComp).ToList(),

                LocatedMarketAreaId = ownedProperty.MarketAreaId,
                LocatedAreaName = locatedAreaName,
                LocatedByCoordinates = false,

                AskSpread = BuildAskSpread(ownedProperty.PurchasePrice, ownedProperty.PurchaseDate, pricing.MidPrice),
            };
        }

        /// <inheritdoc/>
        public async Task<OwnedPropertyPortfolioResponse> GetPortfolioAsync(int radiusMeters, int months, int years)
        {
            var response = new OwnedPropertyPortfolioResponse
            {
                GeneratedAtUtc = DateTime.UtcNow,
                ProjectionYears = years,
            };

            var owned = await _ownedPropertyRepository.GetAllOwnedPropertyAsync();

            // No properties is not an error - it is what the screen shows before you add one.
            if (owned.Count == 0)
            {
                return response;
            }

            var marketAreas = await _marketAreaRepository.GetAllMarketAreasAsync();

            // Demand and the local trend describe a place, not a property, so two flats in the
            // same município share one answer and one read of that município's history.
            var outlooks = new Dictionary<string, AreaOutlook>(StringComparer.OrdinalIgnoreCase);

            foreach (var entity in owned)
            {
                response.Items.Add(await BuildPortfolioItemAsync(
                    entity, marketAreas, outlooks, radiusMeters, months, years, response.GeneratedAtUtc));
            }

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

        /// <summary>The scenario the portfolio totals are added up from.</summary>
        private const string BaseScenarioName = "Base";

        /// <summary>
        /// Prices one owned property from the listings around it and works out what its place is
        /// doing.
        /// </summary>
        private async Task<OwnedPropertyPortfolioItemResponse> BuildPortfolioItemAsync(
            OwnedProperty entity,
            IReadOnlyList<MarketArea> marketAreas,
            Dictionary<string, AreaOutlook> outlooks,
            int radiusMeters,
            int months,
            int years,
            DateTime asOfUtc)
        {
            var ownedProperty = Converter.MapToResponse(entity);
            var placedIn = marketAreas.FirstOrDefault(x => x.Id == ownedProperty.MarketAreaId);

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

                LocatedAreaName = placedIn is null
                    ? string.Empty
                    : string.IsNullOrWhiteSpace(placedIn.Zone) ? placedIn.Town : placedIn.Zone,
                LocatedByCoordinates = false,
            };

            var comps = await GetUsableComparablesAsync(ownedProperty.MarketAreaId, ownedProperty, radiusMeters, months);

            if (comps.Count == 0)
            {
                item.ConfidenceLevel = ValuationConfidence.Low;
                item.ConfidenceNote = "no comparable adverts nearby to price this property";
                item.AskSpread = BuildAskSpread(ownedProperty.PurchasePrice, ownedProperty.PurchaseDate, estimatedAsk: 0);
            }
            else
            {
                var pricing = PriceFromComps(ownedProperty, comps);

                item.MidPrice = pricing.MidPrice;
                item.MinPrice = pricing.MinPrice;
                item.MaxPrice = pricing.MaxPrice;
                item.PricePerM2 = ownedProperty.AreaM2 <= 0 ? 0m : Math.Round(pricing.MidPrice / ownedProperty.AreaM2, 0);
                item.ConfidenceLevel = pricing.Confidence;

                if (item.ConfidenceLevel != ValuationConfidence.High)
                {
                    item.ConfidenceNote = $"checked against {comps.Count} nearby adverts, and there are too few close by to be sure";
                }

                item.AskSpread = BuildAskSpread(ownedProperty.PurchasePrice, ownedProperty.PurchaseDate, pricing.MidPrice);
            }

            var outlook = await GetOutlookAsync(item.District, item.Municipality, outlooks, asOfUtc);

            item.Demand = Converter.MapToDemand(outlook.Demand, DescribePlace(item.Municipality, item.District));

            item.Forecast = Converter.MapToForecast(
                GrowthForecastCalculator.Calculate(item.MidPrice, outlook.Growth, outlook.Trend, years),
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
        /// Comparable listings for one property, held to the same standard as the listings the
        /// premium calculator learns from: broken rows out, re-advertisements of one flat counted
        /// once.
        /// </summary>
        private async Task<List<PropertyListing>> GetUsableComparablesAsync(
            int locatedMarketAreaId, OwnedPropertyResponse ownedProperty, int radiusMeters, int months)
        {
            var comparableListings = await _propertyListingRepository.GetComparablePropertyListingAsync(
                locatedMarketAreaId, ownedProperty.PropertyType, ownedProperty.Typology, ownedProperty.AreaM2,
                ownedProperty.DistanceToBeachMeters, ownedProperty.Latitude, ownedProperty.Longitude, radiusMeters, months);

            return ListingQuality.DistinctProperties(
                comparableListings.Where(x => ListingQuality.IsUsable(x, ListingQuality.NewestSnapshot(x))));
        }

        /// <summary>Below this many nearby comparables, or this close to none, confidence drops a tier.</summary>
        private const int HighConfidenceComparables = 10;

        private const double HighConfidenceMeters = 1_000;
        private const double MediumConfidenceMeters = 5_000;

        /// <summary>Everything a price needs from the comps: the headline number, its range, and how much to trust it.</summary>
        private readonly record struct CompPricing(
            List<PropertyListing> NearestComps,
            decimal MidPrice,
            decimal MinPrice,
            decimal MaxPrice,
            decimal AveragePrice,
            decimal MarketRatePerM2,
            decimal CompPricePerM2P25,
            decimal MedianCompPricePerM2,
            decimal CompPricePerM2P75,
            decimal AverageCompPricePerM2,
            ValuationConfidence Confidence);

        /// <summary>
        /// Prices a property straight off its comparable listings: the headline price is the
        /// weighted median €/m² of the nearest ones, the range is their P25/P75 spread, and
        /// confidence follows how much close evidence backs the number.
        /// </summary>
        private static CompPricing PriceFromComps(OwnedPropertyResponse property, List<PropertyListing> comps)
        {
            var (nearestComps, weightedMedianPricePerM2, weightedAveragePricePerM2) = NearestCompStatistics(property, comps);

            var sortedCompPricesPerM2 = nearestComps
                .Select(x => ListingQuality.NewestSnapshot(x)!.PricePerM2)
                .OrderBy(x => x)
                .ToList();

            // Deliberately quartiles rather than the true min and max: one 2m2 advert at
            // EUR 174,500/m2 would otherwise define the whole band.
            var compPricePerM2P25 = Calculator.Percentile(sortedCompPricesPerM2, 0.25);
            var medianCompPricePerM2 = Calculator.Median(sortedCompPricesPerM2);
            var compPricePerM2P75 = Calculator.Percentile(sortedCompPricesPerM2, 0.75);

            // nearestComps is already ordered nearest-first, so the first entry is the closest one.
            var nearestMeters = nearestComps.Count == 0
                ? double.MaxValue
                : Calculator.CalculateDistanceMeters(
                    (double)property.Latitude, (double)property.Longitude,
                    (double)nearestComps[0].Latitude, (double)nearestComps[0].Longitude);

            return new CompPricing(
                NearestComps: nearestComps,
                MidPrice: Math.Round(weightedMedianPricePerM2 * property.AreaM2),
                MinPrice: Math.Round(compPricePerM2P25 * property.AreaM2),
                MaxPrice: Math.Round(compPricePerM2P75 * property.AreaM2),
                AveragePrice: Math.Round(weightedAveragePricePerM2 * property.AreaM2),
                MarketRatePerM2: weightedMedianPricePerM2,
                CompPricePerM2P25: compPricePerM2P25,
                MedianCompPricePerM2: medianCompPricePerM2,
                CompPricePerM2P75: compPricePerM2P75,
                AverageCompPricePerM2: weightedAveragePricePerM2,
                Confidence: ConfidenceFromComps(nearestComps.Count, nearestMeters));
        }

        /// <summary>
        /// How much to trust a comp-based price: is there evidence NEAR this property, judged on
        /// how many comps back it and how close the nearest one actually is. Internal rather than
        /// private so the thresholds can be tested directly.
        /// </summary>
        internal static ValuationConfidence ConfidenceFromComps(int comparablesUsed, double nearestComparableMeters)
        {
            if (comparablesUsed >= HighConfidenceComparables && nearestComparableMeters <= HighConfidenceMeters)
                return ValuationConfidence.High;

            if (comparablesUsed > 0 && nearestComparableMeters <= MediumConfidenceMeters)
                return ValuationConfidence.Medium;

            return ValuationConfidence.Low;
        }

        /// <summary>
        /// The nearest comparables to one property, and what they ask, weighted by how close each
        /// one actually is.
        ///
        /// Only the nearest handful get a say. Distance weighting alone was not enough: the kernel
        /// still gives a comparable 800m away most of a vote, so three hundred of them drown out
        /// the seventeen next door - which is how "comps alone" read EUR 460,000 for a flat whose
        /// immediate neighbours ask EUR 321,000.
        /// </summary>
        private static (List<PropertyListing> Nearest, decimal WeightedMedianPricePerM2, decimal WeightedAveragePricePerM2)
            NearestCompStatistics(OwnedPropertyResponse ownedProperty, List<PropertyListing> comps)
        {
            const int comparablesUsedForStatistics = 25;

            var nearestComps = OrderedByDistanceFrom(ownedProperty, comps)
                .Take(comparablesUsedForStatistics)
                .ToList();

            var weightedComps = nearestComps.Select(x => (
                    Value: ListingQuality.NewestSnapshot(x)!.PricePerM2,
                    Weight: EvidenceWeightAtMeters(Calculator.CalculateDistanceMeters(
                        (double)ownedProperty.Latitude, (double)ownedProperty.Longitude,
                        (double)x.Latitude, (double)x.Longitude))))
                .ToList();

            return (nearestComps, Calculator.WeightedMedian(weightedComps), Calculator.WeightedAverage(weightedComps));
        }

        /// <summary>The distance at which a comp counts half as much as one next door.</summary>
        private const double NeighbourKernelMeters = 1_000;

        /// <summary>
        /// How much a listing this far away is worth as evidence: full weight next door, half at
        /// <see cref="NeighbourKernelMeters"/>, fading from there.
        /// </summary>
        private static double EvidenceWeightAtMeters(double metres)
        {
            return 1 / (1 + Math.Pow(metres / NeighbourKernelMeters, 2));
        }

        private static IEnumerable<PropertyListing> OrderedByDistanceFrom(
            OwnedPropertyResponse property, List<PropertyListing> comps)
        {
            return comps
                .OrderBy(x => Calculator.CalculateDistanceMeters(
                    (double)property.Latitude, (double)property.Longitude,
                    (double)x.Latitude, (double)x.Longitude));
        }

        /// <summary>Leap years included, so "years held" doesn't drift.</summary>
        private const decimal DaysPerYear = 365.25m;

        /// <summary>
        /// How far the estimated ask has drifted from what was paid. All zeros without a purchase
        /// price - a spread measured against nothing still renders convincingly on screen.
        /// </summary>
        private static AskSpreadSummary BuildAskSpread(
            decimal? purchasePrice, DateTime? purchaseDate, decimal estimatedAsk)
        {
            var paid = purchasePrice ?? 0;
            var spreadAmount = estimatedAsk - paid;
            var spreadPercent = paid > 0 ? spreadAmount / paid * 100 : 0;

            // Fractional years (2.5, not 2) so the per-year maths is accurate.
            // A property saved without a purchase date carries the DateTime default, which reads as
            // two thousand years held. Anything before 1900 is an unset field, not a purchase.
            var yearsHeldExact = purchaseDate.HasValue && purchaseDate.Value.Year > 1900
                ? (decimal)(DateTime.UtcNow - purchaseDate.Value).TotalDays / DaysPerYear
                : 0m;

            var monthsHeldExact = yearsHeldExact * 12;

            return new AskSpreadSummary
            {
                PurchasePrice = paid,
                EstimatedAskingPrice = estimatedAsk,
                SpreadAmount = spreadAmount,
                SpreadPercent = Math.Round(spreadPercent, 2),
                YearsHeld = (int)yearsHeldExact,
                // Compounded, not divided - a linear split overstates the annual rate on
                // anything held more than a year. Under a year, annualising magnifies noise
                // either way, so say nothing rather than 0.
                SpreadPerYearPercent = paid > 0 && estimatedAsk > 0 && yearsHeldExact >= 1
                    ? Math.Round((decimal)Math.Pow((double)(estimatedAsk / paid), 1.0 / (double)yearsHeldExact) * 100 - 100, 2)
                    : null,

                // Per month divides sensibly at any age, so recent buys still show something.
                SpreadPerMonthPercent = paid > 0 && monthsHeldExact > 0
                    ? Math.Round(spreadPercent / monthsHeldExact, 2)
                    : 0,
            };
        }
    }
}
