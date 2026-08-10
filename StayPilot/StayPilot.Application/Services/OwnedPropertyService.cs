
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
        private readonly IPremiumFeatureRepository _premiumFeatureRepository;

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

            // listings prices
            var sortedListingPrices = similarPropertiesRepo.OrderBy(x => x.ListingSnapshots.First().Price).Select(x => x.ListingSnapshots.First().Price).ToList();
            var minListingPrice = sortedListingPrices[0];
            var medianListingPrice = GetMedianValue(sortedListingPrices);
            var maxListingPrice = sortedListingPrices[^1];

            // features price incriments
            var premiumFeaturesRepo = await _premiumFeatureRepository.GetAllPremiumFeaturesAsync();
            var premiumFeatures = premiumFeaturesRepo.Select(x => Converter.MapToResponse(x)).ToList();
            var ownedPropertyFeatures = HasFeatures(ownedProperty);

            var propertyFeaturesList = premiumFeatures.Where(x => ownedPropertyFeatures.Contains(x.Feature)).ToList();
            var propertyFeaturesListFeaturesSum = premiumFeatures.Where(x => ownedPropertyFeatures.Contains(x.Feature)).Sum(x => x.PremiumPercent) / 100m;

            // owned property precies per m2
            var priceBeforeAdjustments = GetMedianValue(sortedListingPricesPerM2) * ownedProperty.AreaM2;

            // Robust range: use the 25th/75th percentile of comp price/m2 instead of the
            // absolute cheapest/priciest, so one freak listing (the 6703 penthouse) can't
            // blow the range wide open. Median still drives the headline mid price.
            var lowPricePerM2 = GetPercentile(sortedListingPricesPerM2, 0.25);
            var medianPricePerM2 = GetMedianValue(sortedListingPricesPerM2);
            var highPricePerM2 = GetPercentile(sortedListingPricesPerM2, 0.75);

            var minOwnedPropertyPrice = lowPricePerM2 * ownedProperty.AreaM2 * (1 + propertyFeaturesListFeaturesSum);
            var medianOwnedPropertyPrice = medianPricePerM2 * ownedProperty.AreaM2 * (1 + propertyFeaturesListFeaturesSum);
            var maxOwnedPropertyPrice = highPricePerM2 * ownedProperty.AreaM2 * (1 + propertyFeaturesListFeaturesSum);

            // comps comparison (same robust percentiles, exposed for transparency)
            var minCompsPricePerM2 = lowPricePerM2;
            var medianCompsPricePerM2 = medianPricePerM2;
            var maxCompsPricePerM2 = highPricePerM2;

            // Mean (average) alongside the median. Shown together on purpose: the gap between
            // them signals a skewed market - a few pricey comps pull the average above the median.
            var averagePricePerM2 = sortedListingPricesPerM2.Average();
            var averageOwnedPropertyPrice = averagePricePerM2 * ownedProperty.AreaM2 * (1 + propertyFeaturesListFeaturesSum);

            var compsCount = similarPropertiesRepo.Count;

            var confidenceLevel = compsCount < 3
                ? ValuationConfidence.Low
                : (compsCount <= 5 ? ValuationConfidence.Medium : ValuationConfidence.High);

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

                Adjustments = premiumFeatures.Where(x => ownedPropertyFeatures.Contains(x.Feature))
                    .Select(x => new ValuationAdjustment
                    {
                        Label = FriendlyFeatureName(x.Feature),
                        Amount = priceBeforeAdjustments * (x.PremiumPercent / 100)
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

        private List<PremiumFeatures> HasFeatures(OwnedPropertyResponse property)
        {
            var premiumFeaturesList = new List<PremiumFeatures>();

            if (property.HasSeaView == true) premiumFeaturesList.Add(PremiumFeatures.HasSeaView);
            if (property.HasGarage == true) premiumFeaturesList.Add(PremiumFeatures.HasGarage);
            if (property.HasCityView == true) premiumFeaturesList.Add(PremiumFeatures.HasCityView);
            if (property.HasSwimmingPool == true) premiumFeaturesList.Add(PremiumFeatures.HasSwimmingPool);
            if (property.HasTerrace == true) premiumFeaturesList.Add(PremiumFeatures.HasTerrace);
            if (property.HasElevator == true) premiumFeaturesList.Add(PremiumFeatures.HasElevator);
            if (property.HasAirConditioning == true) premiumFeaturesList.Add(PremiumFeatures.HasAirConditioning);
            if (property.IsFurnished == true) premiumFeaturesList.Add(PremiumFeatures.IsFurnished);
            if (property.Condition == PropertyCondition.NewBuild == true) premiumFeaturesList.Add(PremiumFeatures.IsNewBuild);
            if (property.Condition == PropertyCondition.Renovated == true) premiumFeaturesList.Add(PremiumFeatures.IsRenovated);

            return premiumFeaturesList;
        }
    }
}
