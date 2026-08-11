
using Abp.Application.Features;
using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Contracts.Response.SubResponse;
using StayPilot.Application.Helpers.Calculators;
using StayPilot.Application.Helpers.Mappers;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Application.Interfaces.Services;
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace StayPilot.Application.Services
{
    public class OwnedPropertyService : IOwnedPropertyService
    {
        private readonly IOwnedPropertyRepository _ownedPropertyRepository;
        private readonly IMarketAreaRepository _marketAreaRepository;
        private readonly IBeachMarkerRepository _beachMarkerRepository;
        private readonly IPropertyListingRepository _propertyListingRepository;

        // No IPremiumFeatureRepository here any more: the valuation reads its feature values
        // straight off the fitted model rather than from the stored PremiumFeature rows. Those
        // rows exist to show the Feature Impact screen; reading them here would have let a stale
        // recalculation quietly change someone's valuation.
        public OwnedPropertyService(IOwnedPropertyRepository ownedPropertyRepository, IMarketAreaRepository marketAreaRepository, IBeachMarkerRepository beachMarkerRepository, IPropertyListingRepository propertyListingRepository)
        {
            _ownedPropertyRepository = ownedPropertyRepository;
            _marketAreaRepository = marketAreaRepository;
            _beachMarkerRepository = beachMarkerRepository;
            _propertyListingRepository = propertyListingRepository;
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

            // Listing prices per M2
            var sortedListingPricesPerM2 = similarPropertiesRepo.OrderBy(x => x.ListingSnapshots.First().PricePerM2).Select(x => x.ListingSnapshots.First().PricePerM2).ToList();

            // The headline price now comes from the valuation model rather than from the median
            // comp nudged by feature percentages. The model is fitted on every listing we have,
            // so it can hold size, typology, condition, beach distance and location still while
            // reading the features - and it then corrects for the specific neighbourhood, which
            // is where most of the accuracy turned out to live. Backtested at ~13% median error
            // against ~18.5% for the comp-median approach this replaces.
            var allListings = await _propertyListingRepository.GetAllListingsForFeaturePremiumCalculationAsync();
            var model = ValuationModel.Fit(allListings);

            var subject = ValuationSubject.FromOwnedProperty(ownedProperty);
            var prediction = model.PredictPricePerM2(subject);

            var medianOwnedPropertyPrice = prediction.PricePerM2 * ownedProperty.AreaM2;

            // Range from the model's own measured error, not from the spread of the comps: a
            // tight cluster of comps does not mean a confident valuation, and a wide one does
            // not mean an uncertain one.
            var spread = (decimal)Math.Exp(model.PredictionSpread);
            var minOwnedPropertyPrice = medianOwnedPropertyPrice / spread;
            var maxOwnedPropertyPrice = medianOwnedPropertyPrice * spread;

            // comps comparison, kept for transparency - what the raw neighbours actually ask.
            var minCompsPricePerM2 = GetPercentile(sortedListingPricesPerM2, 0.25);
            var medianCompsPricePerM2 = GetMedianValue(sortedListingPricesPerM2);
            var maxCompsPricePerM2 = GetPercentile(sortedListingPricesPerM2, 0.75);
            var averagePricePerM2 = sortedListingPricesPerM2.Average();

            // Two plain comp-based figures, kept so the screen can show what the raw neighbours
            // imply next to what the model concluded. Neither is adjusted for features - that is
            // the point of them.
            var priceBeforeAdjustments = medianCompsPricePerM2 * ownedProperty.AreaM2;
            var averageOwnedPropertyPrice = averagePricePerM2 * ownedProperty.AreaM2;

            var compsCount = similarPropertiesRepo.Count;

            // Confidence follows the evidence near THIS property, not the comp count. All the
            // listings collected so far are in the Algarve, so a property anywhere else has no
            // local data at all - and must not come back looking confident.
            var confidenceLevel = ValuationConfidence.Low;

            if (prediction.LocalComparablesUsed > 0 && prediction.NearestComparableMeters <= 5000)
                confidenceLevel = ValuationConfidence.Medium;

            if (prediction.LocalComparablesUsed >= 10 && prediction.NearestComparableMeters <= 1000)
                confidenceLevel = ValuationConfidence.High;

            // Equity: what the house has done since purchase. Compute once here so the total
            // gain and the annualised ROI all use the same numbers.
            var purchasePrice = ownedProperty.PurchasePrice ?? 0;
            var gainAmount = medianOwnedPropertyPrice - purchasePrice;
            var gainPercent = purchasePrice > 0 ? gainAmount / purchasePrice * 100 : 0;
            // Fractional years (e.g. 2.5) so the ROI/year math is accurate; YearsHeld shows whole years.
            var yearsHeldExact = ownedProperty.PurchaseDate.HasValue
                ? (decimal)((DateTime.UtcNow - ownedProperty.PurchaseDate.Value).TotalDays / 365.25)
                : 0m;
            var monthsHeldExact = yearsHeldExact * 12;

            var finalEstimate = new OwnedPropertyAnalysisResponse
            {
                MinPrice = minOwnedPropertyPrice,
                MidPrice = medianOwnedPropertyPrice,
                MaxPrice = maxOwnedPropertyPrice,
                AveragePrice = averageOwnedPropertyPrice,

                ConfidenceLevel = confidenceLevel,
                CompsCount = compsCount,

                MarketRatePerM2 = medianCompsPricePerM2,
                EstimateBeforeAdjustments = priceBeforeAdjustments,

                MinCompPricePerM2 = minCompsPricePerM2,
                MedianCompPricePerM2 = medianCompsPricePerM2,
                MaxCompPricePerM2 = maxCompsPricePerM2,
                AverageCompPricePerM2 = averagePricePerM2,

                // What each feature this property HAS contributes to the estimate above.
                // The model multiplies by (1 + percent) for a feature it has, so the part of
                // the price owed to that feature is what disappears when you divide it back out.
                // Only measurable features are listed - one whose confidence range straddles
                // zero has no contribution worth naming.
                // BeachProximity is skipped on purpose: it is measured per halving of distance,
                // so there is no single amount to attribute without picking a reference point.
                Adjustments = model.FeatureEffects
                    .Where(x => x.IsMeasurable
                                && x.Feature != PremiumFeatures.BeachProximity
                                && ValuationModel.HasFeature(subject, x.Feature))
                    .Select(x => new ValuationAdjustment
                    {
                        Label = FriendlyFeatureName(x.Feature),
                        Amount = Math.Round(
                            medianOwnedPropertyPrice * (1 - 1 / (1 + FeaturePercentFor(model, subject, x) / 100)), 0)
                    }).ToList(),

                Comps = similarPropertiesRepo.Select(x => new ValuationComp
                {
                    AreaM2 = x.AreaM2,
                    PricePerM2 = x.ListingSnapshots.First().PricePerM2,
                    DistanceToBeachMeters = x.DistanceToBeachMeters,
                    Typology = x.Typology,
                    SnapshotDateUtc = x.ListingSnapshots.First().SnapshotDateUtc,
                }).ToList(),

                Equity = new EquitySummary
                {
                    PurchasePrice = purchasePrice,
                    CurrentEstimate = medianOwnedPropertyPrice,
                    GainAmount = gainAmount,
                    GainPercent = Math.Round(gainPercent, 2),
                    YearsHeld = (int)yearsHeldExact,
                    // ROI per year = total gain % spread over the years held (simple annualisation).
                    // Needs a full year held AND a purchase price, else annualising is meaningless -> 0.
                    RoiPerYear = (purchasePrice > 0 && yearsHeldExact >= 1)
                        ? Math.Round(gainPercent / yearsHeldExact, 2)
                        : 0,
                    // ROI per month: same idea, finer granularity - so recent buys (held < 1 year)
                    // still show a number instead of 0. Only needs some holding time > 0.
                    RoiPerMonth = (purchasePrice > 0 && monthsHeldExact > 0)
                        ? Math.Round(gainPercent / monthsHeldExact, 2)
                        : 0,
                }
            };

            return finalEstimate;
        }

        public async Task<List<OwnedPropertyResponse>> GetAllOwnedPropertiesAsync()
        {
            var domainOwnedProperties = await _ownedPropertyRepository.GetAllOwnedPropertyAsync();

            // Reuse the same mapper every other method here uses
            return domainOwnedProperties
                .Select(x => Converter.MapToResponse(x))
                .ToList();
        }

        /// <summary>
        /// What a feature is worth to THIS property, rather than to the market on average.
        ///
        /// Only the sea view differs: it is worth roughly a quarter more on the beachfront and
        /// almost nothing several kilometres inland, so a beachfront flat credited with the
        /// market-average figure would be badly undersold. The Feature Impact screen still shows
        /// the average - that is a market summary, this is one specific property.
        /// </summary>
        private static decimal FeaturePercentFor(ValuationModel model, ValuationSubject subject, FeatureEffect effect)
        {
            if (effect.Feature != PremiumFeatures.HasSeaView || subject.DistanceToBeachMeters is null)
                return effect.Percent;

            return model.SeaViewPercentAt(subject.DistanceToBeachMeters.Value);
        }

        private decimal GetMedianValue(List<decimal> list)
        {
            var count = list.Count;
            return count %2 != 0 ? list[count / 2] : (list[(count / 2)] + list[(count / 2) - 1]) / 2;
        }

        /// <summary>
        /// Value at a given percentile (0.0-1.0) of an ascending-sorted list, interpolating
        /// between neighbours. p=0.5 is the median, p=0.25 the lower quartile. Used instead
        /// of raw min/max so a single freak listing can't define the range.
        /// </summary>
        private decimal GetPercentile(List<decimal> sortedAscending, double percentile)
        {
            if (sortedAscending.Count == 0) return 0;
            if (sortedAscending.Count == 1) return sortedAscending[0];

            var rank = percentile * (sortedAscending.Count - 1);
            var lowIndex = (int)Math.Floor(rank);
            var highIndex = (int)Math.Ceiling(rank);
            if (lowIndex == highIndex) return sortedAscending[lowIndex];

            var weight = (decimal)(rank - lowIndex);
            return sortedAscending[lowIndex] * (1 - weight) + sortedAscending[highIndex] * weight;
        }

        /// <summary>
        /// Turns a feature enum into a human label: drops the "Has"/"Is" prefix and spaces out
        /// the camel case. E.g. HasSeaView -> "Sea View", IsNewBuild -> "New Build".
        /// </summary>
        private static string FriendlyFeatureName(PremiumFeatures feature)
        {
            var name = feature.ToString();
            if (name.StartsWith("Has")) name = name.Substring(3);
            else if (name.StartsWith("Is")) name = name.Substring(2);

            // Insert a space before each capital that follows a lower-case letter.
            return Regex.Replace(name, "(?<=[a-z])([A-Z])", " $1");
        }

    }
}
