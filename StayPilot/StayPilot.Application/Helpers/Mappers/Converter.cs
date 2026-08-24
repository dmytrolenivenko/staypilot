using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Contracts.Response.SubResponse;
using StayPilot.Application.Helpers.Calculators;
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Helpers.Mappers
{
    /// <summary>
    /// Maps between database entities and the request/response shapes.
    /// Each method just copies fields from one object into another.
    /// </summary>
    public class Converter
    {
        /// <summary>
        /// Turn a PropertyListing entity (plus its snapshot) into the response we send back.
        /// The snapshot is optional; pass null if there is none.
        /// </summary>
        public static PropertyListingResponse MapToResponse(PropertyListing property, ListingSnapshot? listingSnapshot = null)
        {

            // The market area must be loaded first, because we copy its fields below.
            if (property.MarketArea == null)
            {
                throw new InvalidOperationException("MarketArea must be loaded before mapping.");
            }

            return new PropertyListingResponse
            {
                Id = property.Id,
                MarketAreaId = property.MarketAreaId,
                MarketAreaDistrict = property.MarketArea.District,
                MarketAreaMunicipality = property.MarketArea.Municipality,
                MarketAreaTown = property.MarketArea.Town,
                MarketAreaZone = property.MarketArea.Zone ?? string.Empty,
                PropertyType = property.PropertyType,
                Typology = property.Typology,
                SourceName = property.SourceName,
                SourceUrl = property.SourceUrl,
                AreaM2 = property.AreaM2,
                Bathrooms = property.Bathrooms,
                Floor = property.Floor,
                TotalFloors = property.TotalFloors,
                HasElevator = property.HasElevator,
                HasAirConditioning = property.HasAirConditioning,
                Condition = property.Condition,
                ConstructionYear = property.ConstructionYear,
                DistanceToBeachMeters = property.DistanceToBeachMeters,
                NearestBeachMarkerId = property.NearestBeachMarkerId,
                NearestBeachName = property.NearestBeachName,
                RenovationYear = property.RenovationYear,
                BalconyCount = property.BalconyCount,
                HasTerrace = property.HasTerrace,
                HasGarage = property.HasGarage,
                HasParking = property.HasParking,
                HasSwimmingPool = property.HasSwimmingPool,
                IsFurnished = property.IsFurnished,
                HasSeaView = property.HasSeaView,
                HasCityView = property.HasCityView,
                Latitude = property.Latitude,
                Longitude = property.Longitude,
                EnergyCertificate = property.EnergyCertificate,
                Notes = property.Notes,
                ListingSnapshot = listingSnapshot is not null ? MapToResponse(listingSnapshot) : null
            };
        }

        /// <summary>
        /// Turn a PropertyListingRequest (data from the caller) into a PropertyListing entity to save.
        /// </summary>
        public static PropertyListing MapToEntity(PropertyListingRequest property)
        {
            return new PropertyListing
            {
                PropertyType = property.PropertyType,
                Typology = property.Typology,
                SourceName = property.SourceName,
                SourceUrl = property.SourceUrl,
                AreaM2 = property.AreaM2,
                Bathrooms = property.Bathrooms,
                Floor = property.Floor,
                TotalFloors = property.TotalFloors,
                HasElevator = property.HasElevator,
                HasAirConditioning = property.HasAirConditioning,
                Condition = property.Condition,
                ConstructionYear = property.ConstructionYear,
                RenovationYear = property.RenovationYear,
                BalconyCount = property.BalconyCount,
                HasTerrace = property.HasTerrace,
                HasGarage = property.HasGarage,
                HasParking = property.HasParking,
                HasSwimmingPool = property.HasSwimmingPool,
                IsFurnished = property.IsFurnished,
                HasSeaView = property.HasSeaView,
                HasCityView = property.HasCityView,
                Latitude = property.Latitude,
                Longitude = property.Longitude,
                EnergyCertificate = property.EnergyCertificate,
                Notes = property.Notes,
            };
        }

        /// <summary>
        /// Turn a ListingSnapshotRequest (data from the caller) into a ListingSnapshot entity to save.
        /// </summary>
        public static ListingSnapshot MapToEntity(ListingSnapshotRequest snapshot)
        {
            return new ListingSnapshot
            {
                PropertyListingId = snapshot.PropertyListingId,
                Price = snapshot.Price,
                PricePerM2 = snapshot.PricePerM2,
                Status = snapshot.Status,
                SnapshotDateUtc = snapshot.SnapshotDateUtc
            };
        }

        /// <summary>
        /// Turn a ListingSnapshot entity into the snapshot response we send back.
        /// </summary>
        public static ListingSnapshotResponse MapToResponse(ListingSnapshot snapshot)
        {
            return new ListingSnapshotResponse
            {
                Id = snapshot.Id,
                PropertyListingId = snapshot.PropertyListingId,
                Price = snapshot.Price,
                PricePerM2 = snapshot.PricePerM2,
                Status = snapshot.Status,
                SnapshotDateUtc = snapshot.SnapshotDateUtc
            };
        }

        /// <summary>
        /// Turn a MarketArea entity into the market area response we send back.
        /// </summary>
        public static MarketAreaResponse MapToResponse(MarketArea marketArea)
        {
            return new MarketAreaResponse
            {
                Id = marketArea.Id,
                Country = marketArea.Country,
                District = marketArea.District,
                Municipality = marketArea.Municipality,
                Town = marketArea.Town,
                Zone = marketArea.Zone,
                Notes = marketArea.Notes
            };
        }

        /// <summary>
        /// Puts the request's values onto an existing property, for Update only.
        /// Name/PropertyType/Typology/AreaM2/Bathrooms are always sent (they are
        /// required), so we always set them. Everything else is optional: we only
        /// change it when the caller actually sent a value, so a request that
        /// leaves a field out does not blank the value already saved.
        /// </summary>
        public static void ApplyUpdates(OwnedProperty entity, OwnedPropertyRequest request)
        {
            entity.Name = request.Name;
            entity.PropertyType = request.PropertyType;
            entity.Typology = request.Typology;
            entity.AreaM2 = request.AreaM2;
            entity.Bathrooms = request.Bathrooms;

            if (request.Floor is not null) entity.Floor = request.Floor;
            if (request.TotalFloors is not null) entity.TotalFloors = request.TotalFloors;
            if (request.HasElevator is not null) entity.HasElevator = request.HasElevator;
            if (request.HasAirConditioning is not null) entity.HasAirConditioning = request.HasAirConditioning;
            if (request.Condition is not null) entity.Condition = request.Condition.Value;
            if (request.ConstructionYear is not null) entity.ConstructionYear = request.ConstructionYear;
            if (request.RenovationYear is not null) entity.RenovationYear = request.RenovationYear;
            if (request.RenovationInvestment is not null) entity.RenovationInvestment = request.RenovationInvestment;
            if (request.BalconyCount is not null) entity.BalconyCount = request.BalconyCount.Value;
            if (request.HasTerrace is not null) entity.HasTerrace = request.HasTerrace.Value;
            if (request.HasGarage is not null) entity.HasGarage = request.HasGarage.Value;
            if (request.HasParking is not null) entity.HasParking = request.HasParking.Value;
            if (request.HasSwimmingPool is not null) entity.HasSwimmingPool = request.HasSwimmingPool.Value;
            if (request.IsFurnished is not null) entity.IsFurnished = request.IsFurnished.Value;
            if (request.HasSeaView is not null) entity.HasSeaView = request.HasSeaView.Value;
            if (request.HasCityView is not null) entity.HasCityView = request.HasCityView.Value;
            if (request.Latitude is not null) entity.Latitude = request.Latitude;
            if (request.Longitude is not null) entity.Longitude = request.Longitude;
            if (request.EnergyCertificate is not null) entity.EnergyCertificate = request.EnergyCertificate;
            if (request.Notes is not null) entity.Notes = request.Notes;
            if (request.PurchasePrice is not null) entity.PurchasePrice = request.PurchasePrice.Value;
            if (request.PurchaseDate is not null) entity.PurchaseDate = request.PurchaseDate.Value;
        }

        public static OwnedProperty MapToEntity(OwnedPropertyRequest request)
        {
            return new OwnedProperty
            {
                Name = request.Name,
                PropertyType = request.PropertyType,
                Typology = request.Typology,
                AreaM2 = request.AreaM2,
                Bathrooms = request.Bathrooms,
                Floor = request.Floor,
                TotalFloors = request.TotalFloors,
                HasElevator = request.HasElevator,
                Condition = request.Condition ?? default,
                ConstructionYear = request.ConstructionYear,
                RenovationYear = request.RenovationYear,
                RenovationInvestment = request.RenovationInvestment,
                BalconyCount = request.BalconyCount ?? 0,
                HasTerrace = request.HasTerrace ?? false,
                HasGarage = request.HasGarage ?? false,
                HasParking = request.HasParking ?? false,
                HasSwimmingPool = request.HasSwimmingPool ?? false,
                IsFurnished = request.IsFurnished ?? false,
                HasAirConditioning = request.HasAirConditioning ?? false,
                HasSeaView = request.HasSeaView ?? false,
                HasCityView = request.HasCityView ?? false,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                EnergyCertificate = request.EnergyCertificate,
                Notes = request.Notes,
                PurchasePrice = request.PurchasePrice ?? 0,
                PurchaseDate = request.PurchaseDate ?? default
            };
        }

        public static OwnedPropertyResponse MapToResponse(OwnedProperty entity)
        {
            return new OwnedPropertyResponse
            {
                Id = entity.Id,
                Name = entity.Name,
                MarketAreaId = entity.MarketAreaId,
                PurchasePrice = entity.PurchasePrice,
                PurchaseDate = entity.PurchaseDate,
                PropertyType = entity.PropertyType,
                Typology = entity.Typology,
                AreaM2 = entity.AreaM2,
                Bathrooms = entity.Bathrooms ?? 0,
                Floor = entity.Floor,
                TotalFloors = entity.TotalFloors,
                HasElevator = entity.HasElevator,
                HasAirConditioning = entity.HasAirConditioning,
                Condition = entity.Condition,
                ConstructionYear = entity.ConstructionYear,
                RenovationYear = entity.RenovationYear,
                RenovationInvestment = entity.RenovationInvestment,
                BalconyCount = entity.BalconyCount,
                HasTerrace = entity.HasTerrace,
                HasGarage = entity.HasGarage,
                HasParking = entity.HasParking,
                HasSwimmingPool = entity.HasSwimmingPool,
                IsFurnished = entity.IsFurnished,
                HasSeaView = entity.HasSeaView,
                HasCityView = entity.HasCityView,
                Latitude = entity.Latitude,
                Longitude = entity.Longitude,
                EnergyCertificate = entity.EnergyCertificate,
                Notes = entity.Notes,
                DistanceToBeachMeters = entity.DistanceToBeachMeters,
            };

        }

        public static PremiumFeatureResponse MapToResponse(PremiumFeature entity)
        {
            var response = new PremiumFeatureResponse
            {
                Feature = entity.Feature,
                PremiumPercent = entity.PremiumPercent,
                LowerBoundPercent = entity.LowerBoundPercent,
                UpperBoundPercent = entity.UpperBoundPercent,
                SampleSize = entity.SampleSize,
                ListingsWithFeature = entity.ListingsWithFeature,
                MaximumPercent = entity.MaximumPercent,
                MaximumBasis = entity.MaximumBasis,
                Basis = entity.Basis,
                // Measurable only when the whole confidence range sits on one side of zero.
                IsMeasurable = entity.LowerBoundPercent > 0 || entity.UpperBoundPercent < 0,

                CalculatedAtUtc = entity.CalculatedAtUtc,
            };

            return response;
        }

        /// <summary>
        /// A stored premium row read back as a feature effect, so a valuation can price a
        /// property from the percentages already measured instead of measuring them again.
        /// </summary>
        public static FeatureEffect MapToFeatureEffect(PremiumFeature entity)
        {
            return new FeatureEffect
            {
                Feature = entity.Feature,
                Percent = entity.PremiumPercent,
                LowerPercent = entity.LowerBoundPercent,
                UpperPercent = entity.UpperBoundPercent,
                ListingsWithFeature = entity.ListingsWithFeature,
                MaximumPercent = entity.MaximumPercent,
                MaximumBasis = entity.MaximumBasis,
                Basis = entity.Basis,
            };
        }

        /// <summary>
        /// One comparable listing for the valuation screen: is it really comparable, and how
        /// stale is it.
        /// </summary>
        public static ValuationComp MapToComp(PropertyListing listing)
        {
            var snapshot = listing.ListingSnapshots.First();

            return new ValuationComp
            {
                AreaM2 = listing.AreaM2,
                PricePerM2 = snapshot.PricePerM2,
                DistanceToBeachMeters = listing.DistanceToBeachMeters,
                Typology = listing.Typology,
                SnapshotDateUtc = snapshot.SnapshotDateUtc,
            };
        }

        /// <summary>
        /// One place's price numbers, in the shape we send back.
        /// </summary>
        public static MarketAreaStatsResponse MapToResponse(MarketAreaStats stats)
        {
            // Rounded once, here, and the discount below derives from this rounded pair. Every
            // screen prints these to the euro, so subtracting the unrounded originals is how
            // "5,794 - 2,869" gets displayed as 2,926 - arithmetic a reader can check by eye
            // and find wrong. Same rule the build-cost receipt already follows.
            var moveInPerM2 = stats.MoveInMedianPricePerM2 is null
                ? (decimal?)null
                : Math.Round(stats.MoveInMedianPricePerM2.Value);

            var projectPerM2 = stats.ProjectMedianPricePerM2 is null
                ? (decimal?)null
                : Math.Round(stats.ProjectMedianPricePerM2.Value);

            return new MarketAreaStatsResponse
            {
                Level = stats.Level,
                District = stats.District,
                Municipality = stats.Municipality,
                Town = stats.Town,
                DisplayName = BuildPlaceName(stats),
                ListingCount = stats.ListingCount,
                MedianPricePerM2 = stats.MedianPricePerM2,
                MedianAreaM2 = stats.MedianAreaM2,
                BelowEstimateCount = stats.BelowEstimateCount,
                ProjectCount = stats.ProjectCount,
                ProjectByConditionCount = stats.ProjectByConditionCount,
                ProjectByEnergyCount = stats.ProjectByEnergyCount,
                ProjectMedianPricePerM2 = projectPerM2,
                ProjectMedianAreaM2 = stats.ProjectMedianAreaM2,
                ProjectP25PricePerM2 = stats.ProjectP25PricePerM2,
                ProjectP75PricePerM2 = stats.ProjectP75PricePerM2,
                MoveInCount = stats.MoveInCount,
                MoveInMedianPricePerM2 = moveInPerM2,
                MoveInMedianAreaM2 = stats.MoveInMedianAreaM2,
                MoveInP25PricePerM2 = stats.MoveInP25PricePerM2,
                MoveInP75PricePerM2 = stats.MoveInP75PricePerM2,
                UnclassifiedCount = stats.UnclassifiedCount,

                // Only a discount when we measured both sides. One side alone tells you nothing
                // about the gap, and a zero here would read as "no discount" instead of "unknown".
                RenovationDiscountPerM2 = moveInPerM2 is null || projectPerM2 is null
                    ? null
                    : moveInPerM2 - projectPerM2,

                RenovationEvidence = BuildRenovationEvidence(stats),

                CalculatedAtUtc = stats.CalculatedAtUtc,
            };
        }

        /// <summary>
        /// The fewest projects before a discount is worth calling measured. Ten, matching the
        /// threshold the renovation screen already marks a row "thin" at.
        /// </summary>
        private const int ProjectsForConfidence = 10;

        /// <summary>
        /// How much the two spreads may overlap before the discount stops being a finding.
        /// Half: past that, most project stock here asks what finished stock asks, and the gap
        /// between the two medians is describing the sample rather than the market.
        /// </summary>
        private const decimal MaximumUsefulOverlapPercent = 50m;

        /// <summary>
        /// The share of a place's stock that has to get a verdict before the discount can be read
        /// as being about that place. A fifth is low, deliberately - most listings carry neither a
        /// condition nor a certificate, so demanding a majority would empty the screen.
        /// </summary>
        private const decimal MinimumClassifiedSharePercent = 20m;

        /// <summary>
        /// Why the discount for this place should or should not be believed. Null when there is no
        /// discount to judge, which the screen already handles by leaving the place out.
        /// </summary>
        private static RenovationEvidenceResponse? BuildRenovationEvidence(MarketAreaStats stats)
        {
            if (stats.ProjectMedianPricePerM2 is null || stats.MoveInMedianPricePerM2 is null)
            {
                return null;
            }

            var overlapPercent = SpreadOverlapPercent(stats);

            var classifiedSharePercent = stats.ListingCount == 0
                ? 0m
                : decimal.Round((decimal)(stats.ProjectCount + stats.MoveInCount) / stats.ListingCount * 100m, 1);

            var evidence = new RenovationEvidenceResponse
            {
                SpreadOverlapPercent = overlapPercent,
                ClassifiedSharePercent = classifiedSharePercent
            };

            // Ordered by how badly each failure undermines the number, worst first, so the reason
            // names the thing most worth fixing rather than the first thing checked.
            if (stats.ProjectCount < ProjectsForConfidence)
            {
                evidence.Confidence = ValuationConfidence.Low;
                evidence.Reason = $"only {stats.ProjectCount} project listings here";

                return evidence;
            }

            if (overlapPercent > MaximumUsefulOverlapPercent)
            {
                evidence.Confidence = ValuationConfidence.Low;
                evidence.Reason =
                    $"project and finished prices overlap {overlapPercent:0}% - most project stock here asks what finished stock asks";

                return evidence;
            }

            if (classifiedSharePercent < MinimumClassifiedSharePercent)
            {
                evidence.Confidence = ValuationConfidence.Medium;
                evidence.Reason =
                    $"resting on {classifiedSharePercent:0}% of the listings here - the rest carry no condition or certificate";

                return evidence;
            }

            evidence.Confidence = ValuationConfidence.High;
            evidence.Reason =
                $"{stats.ProjectCount} projects against {stats.MoveInCount} finished, spreads {overlapPercent:0}% apart";

            return evidence;
        }

        /// <summary>
        /// How much of the middle half of the project prices also falls inside the middle half of
        /// the move-in prices, against the narrower of the two.
        ///
        /// Measured against the narrower one on purpose: a tight project spread sitting wholly
        /// inside a wide finished spread is total overlap, and dividing by the wide one would
        /// report it as partial.
        /// </summary>
        private static decimal SpreadOverlapPercent(MarketAreaStats stats)
        {
            if (stats.ProjectP25PricePerM2 is null || stats.ProjectP75PricePerM2 is null
                || stats.MoveInP25PricePerM2 is null || stats.MoveInP75PricePerM2 is null)
            {
                // No spread to compare. Reported as fully overlapping rather than as zero, so a
                // missing measurement can never be read as a clean separation.
                return 100m;
            }

            var overlapLow = Math.Max(stats.ProjectP25PricePerM2.Value, stats.MoveInP25PricePerM2.Value);
            var overlapHigh = Math.Min(stats.ProjectP75PricePerM2.Value, stats.MoveInP75PricePerM2.Value);
            var overlap = overlapHigh - overlapLow;

            if (overlap <= 0)
            {
                return 0m;
            }

            var narrower = Math.Min(
                stats.ProjectP75PricePerM2.Value - stats.ProjectP25PricePerM2.Value,
                stats.MoveInP75PricePerM2.Value - stats.MoveInP25PricePerM2.Value);

            // Both spreads are a single price. They overlap iff they are the same price, and the
            // one above already established that they are.
            if (narrower <= 0)
            {
                return 100m;
            }

            return decimal.Round(Math.Min(overlap / narrower * 100m, 100m), 1);
        }

        /// <summary>
        /// The best a budget reaches in one place, from that place's typology rows.
        /// </summary>
        public static MarketAreaBudgetItemResponse MapToBudgetItem(MarketAreaStats stats, MarketAreaTypologyStats affordable)
        {
            return new MarketAreaBudgetItemResponse
            {
                Level = stats.Level,
                DisplayName = BuildPlaceName(stats),
                District = stats.District,
                Municipality = stats.Municipality,
                Town = stats.Town,
                BestTypology = affordable.Typology,
                MedianPrice = affordable.MedianPrice,
                MedianAreaM2 = affordable.MedianAreaM2,
                MedianPricePerM2 = affordable.MedianPricePerM2,
                TypologyListingCount = affordable.ListingCount,
                ListingCount = stats.ListingCount,
            };
        }

        /// <summary>
        /// One typology a budget reaches, as an alternative to the headline answer.
        /// </summary>
        public static MarketAreaBudgetTypologyResponse MapToBudgetTypology(MarketAreaTypologyStats typology)
        {
            return new MarketAreaBudgetTypologyResponse
            {
                Typology = typology.Typology,
                MedianPrice = typology.MedianPrice,
                MedianAreaM2 = typology.MedianAreaM2,
                MedianPricePerM2 = typology.MedianPricePerM2,
                ListingCount = typology.ListingCount
            };
        }

        /// <summary>
        /// The place written out for a human, without the parent in brackets. Used where the
        /// parent is already obvious from the row, like both halves of a neighbour pair.
        /// </summary>
        public static string PlaceName(MarketAreaStats stats)
        {
            return BuildPlaceName(stats);
        }

        /// <summary>
        /// One half of a neighbour pair, with the place broken into its parts so the screen can
        /// name the grain instead of leaving a bracket to be guessed at.
        /// </summary>
        /// <param name="stats">The place.</param>
        /// <param name="pricePerM2">
        /// The price the pair was actually compared on. The place's overall median normally, or
        /// one typology's median when the caller narrowed the comparison to a typology - passed
        /// in rather than read off <paramref name="stats"/> so the number on screen is always the
        /// number the gap was worked out from.
        /// </param>
        /// <param name="listingCount">How many listings that price rests on, on the same basis.</param>
        public static NeighbourGapPlaceResponse MapToGapPlace(
            MarketAreaStats stats, decimal pricePerM2, int listingCount)
        {
            return new NeighbourGapPlaceResponse
            {
                Level = stats.Level,
                District = stats.District,
                Municipality = stats.Municipality,
                Town = stats.Town,
                DisplayName = BuildPlaceName(stats),
                MedianPricePerM2 = pricePerM2,
                ListingCount = listingCount,
                AllStockPricePerM2 = stats.MedianPricePerM2,
                AllStockListingCount = stats.ListingCount
            };
        }

        /// <summary>
        /// The place written out for a human, with its parent in brackets so two places sharing
        /// a name stay apart: "Odivelas (Beja)" is not "Odivelas (Lisboa)".
        /// </summary>
        private static string BuildPlaceName(MarketAreaStats stats)
        {
            return stats.Level switch
            {
                AreaLevel.District => stats.District,
                AreaLevel.Municipality => $"{stats.Municipality} ({stats.District})",
                _ => $"{stats.Town} ({stats.Municipality})"
            };
        }

        /// <summary>
        /// Turns the demand score into the block the screen prints, adding the place name -
        /// the calculator scores listings and has no idea which place they came from.
        /// </summary>
        public static AreaDemandResponse MapToDemand(DemandCalculator.DemandOutcome outcome, string placeName)
        {
            return new AreaDemandResponse
            {
                Level = outcome.Level,
                Score = outcome.Score,
                IsMeasurable = outcome.IsMeasurable,
                PlaceName = placeName,
                MedianDaysOnMarket = outcome.MedianDaysOnMarket is null ? null : Math.Round(outcome.MedianDaysOnMarket.Value, 0),
                DaysMeasuredOnSold = outcome.DaysMeasuredOnSold,
                DaysScore = outcome.DaysScore is null ? null : Math.Round(outcome.DaysScore.Value, 1),
                NewListingsRecent = outcome.NewListingsRecent,
                NewListingsPrevious = outcome.NewListingsPrevious,
                SupplyChangePercent = outcome.SupplyChangePercent,
                SupplyScore = outcome.SupplyScore is null ? null : Math.Round(outcome.SupplyScore.Value, 1),
                SampleSize = outcome.SampleSize,
                CollectionSpanDays = outcome.CollectionSpanDays,
                Reason = outcome.Reason,
            };
        }

        /// <summary>
        /// Turns the forecast into the block the screen prints. The two rates stay apart all
        /// the way to the wire, so a projection can be taken apart wherever it is read.
        /// </summary>
        public static GrowthForecastResponse MapToForecast(GrowthForecastCalculator.Forecast forecast, string seededDistrict, int years)
        {
            return new GrowthForecastResponse
            {
                SeededAnnualPercent = forecast.SeededAnnualPercent,
                SeededSource = forecast.SeededSource,
                // The empty district is the national fallback row, which needs a name on screen.
                SeededDistrict = string.IsNullOrWhiteSpace(seededDistrict) ? "Portugal (national)" : seededDistrict,
                LocalAnnualPercent = forecast.LocalAnnualPercent,
                LocalWeightPercent = forecast.LocalWeightPercent,
                LocalWasCapped = forecast.Trend.WasCapped,
                LocalSnapshotCount = forecast.Trend.SnapshotCount,
                LocalSpanDays = forecast.Trend.SpanDays,
                LocalMonthsObserved = forecast.Trend.MonthsObserved,
                LocalReason = forecast.Trend.Reason,
                BlendedAnnualPercent = forecast.BlendedAnnualPercent,
                Years = years,
                Scenarios = forecast.Scenarios.Select(x => new GrowthScenarioResponse
                {
                    Name = x.Name,
                    AnnualPercent = x.AnnualPercent,
                    NextYearValue = x.Values.Count > 1 ? x.Values[1] : x.Values[0],
                    FinalYearValue = x.Values[^1],
                    Values = x.Values.ToList(),
                }).ToList(),
            };
        }
    }
}
