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
    /// <inheritdoc/>
    public class MarketAreaStatsService : IMarketAreaStatsService
    {
        private readonly IMarketAreaStatsRepository _marketAreaStatsRepo;
        private readonly IPropertyListingRepository _propertyListingRepo;

        public MarketAreaStatsService(IMarketAreaStatsRepository marketAreaStatsRepo, IPropertyListingRepository propertyListingRepo)
        {
            _marketAreaStatsRepo = marketAreaStatsRepo;
            _propertyListingRepo = propertyListingRepo;
        }

        /// <inheritdoc/>
        public async Task<RecalculateMarketAreaStatsResponse> RecalculateMarketAreaStatsAsync()
        {
            var response = new RecalculateMarketAreaStatsResponse();

            // Same load the feature premiums use: every listing, with its market area and its
            // newest price. Reused rather than copied - the shape needed is identical.
            var allListings = await _propertyListingRepo.GetAllListingsForFeaturePremiumCalculationAsync();

            var rows = MarketAreaStatsCalculator.Calculate(allListings);

            if (rows.Count == 0)
            {
                // Nothing could be placed or priced. Say so instead of wiping the table and
                // leaving the screens empty with no explanation.
                response.AddError(ErrorCode.NotEnoughListingsForStats, allListings.Count.ToString());

                return response;
            }

            // Overwrite, not append: exactly one current set of rows. The typology children go
            // with their parents, because the configuration cascades the delete.
            var previous = await _marketAreaStatsRepo.GetAllMarketAreaStatsAsync();
            _marketAreaStatsRepo.RemoveMarketAreaStats(previous);

            await _marketAreaStatsRepo.AddMarketAreaStatsAsync(rows);
            await _marketAreaStatsRepo.SaveChangesAsync();

            // Every usable listing lands in exactly one district row, so the district rows added
            // up is how many listings the run actually used.
            response.ListingsUsed = rows
                .Where(x => x.Level == AreaLevel.District)
                .Sum(x => x.ListingCount);

            response.RowsWritten = rows.Count;
            response.CalculatedAtUtc = rows[0].CalculatedAtUtc;

            return response;
        }

        /// <inheritdoc/>
        public async Task<MarketAreaLeaderboardResponse> GetLeaderboardAsync(MarketAreaLeaderboardRequest request)
        {
            var rows = await _marketAreaStatsRepo.GetLeaderboardAsync(request.Level, request.MinListings);

            return new MarketAreaLeaderboardResponse
            {
                Items = rows.Select(Converter.MapToResponse).ToList(),
                CalculatedAtUtc = LastCalculatedAt(rows)
            };
        }

        /// <inheritdoc/>
        public async Task<MarketAreaBudgetResponse> GetBudgetRankingAsync(MarketAreaBudgetRequest request)
        {
            var rows = await _marketAreaStatsRepo.GetWithTypologiesAsync(request.Level, request.MinListings);

            var response = new MarketAreaBudgetResponse
            {
                Budget = request.Budget,
                CalculatedAtUtc = LastCalculatedAt(rows)
            };

            foreach (var place in rows)
            {
                var affordable = BestWithinBudget(place, request.Budget);

                // No typology here usually sells for the budget, so the place is left out
                // entirely rather than listed with an empty answer.
                if (affordable is null)
                {
                    continue;
                }

                response.Items.Add(Converter.MapToBudgetItem(place, affordable));
            }

            return response;
        }

        /// <inheritdoc/>
        public async Task<MarketAreaNeighbourGapResponse> GetNeighbourGapsAsync(MarketAreaNeighbourGapRequest request)
        {
            var rows = await _marketAreaStatsRepo.GetLeaderboardAsync(request.Level, request.MinListings);

            // A place with no coordinates cannot be anybody's neighbour.
            var placeable = rows
                .Where(x => x.CentroidLatitude.HasValue && x.CentroidLongitude.HasValue)
                .ToList();

            var response = new MarketAreaNeighbourGapResponse
            {
                CalculatedAtUtc = LastCalculatedAt(rows)
            };

            // Each pair once: the inner loop starts past the outer one, so A/B is compared but
            // B/A is not, and nothing is compared with itself.
            for (var first = 0; first < placeable.Count; first++)
            {
                for (var second = first + 1; second < placeable.Count; second++)
                {
                    var gap = BuildGap(placeable[first], placeable[second], request);

                    if (gap is not null)
                    {
                        response.Items.Add(gap);
                    }
                }
            }

            response.Items = response.Items
                .OrderByDescending(x => x.GapPercent)
                .ToList();

            return response;
        }

        /// <summary>
        /// The most rooms this place usually sells for the budget, or null when nothing does.
        ///
        /// Judged on what a typology usually costs here, not on the cheapest advert in it: the
        /// cheapest T3 in Cascais being within reach does not make Cascais a place where the
        /// budget buys a T3.
        /// </summary>
        private static MarketAreaTypologyStats? BestWithinBudget(MarketAreaStats place, decimal budget)
        {
            return place.TypologyStats
                .Where(x => x.MedianPrice <= budget)
                .OrderByDescending(x => x.Typology)
                .ThenByDescending(x => x.MedianAreaM2)
                .FirstOrDefault();
        }

        /// <summary>
        /// One pair of places as a gap, or null when they are too far apart, too alike in price,
        /// or a pair we could not measure.
        /// </summary>
        private static NeighbourGapResponse? BuildGap(
            MarketAreaStats first, MarketAreaStats second, MarketAreaNeighbourGapRequest request)
        {
            var distanceMeters = Calculator.CalculateDistanceMeters(
                (double)first.CentroidLatitude!.Value,
                (double)first.CentroidLongitude!.Value,
                (double)second.CentroidLatitude!.Value,
                (double)second.CentroidLongitude!.Value);

            var distanceKm = (decimal)(distanceMeters / 1000);

            if (distanceKm > request.MaxDistanceKm)
            {
                return null;
            }

            // Sort the pair by price so the response always reads "dear place -> cheaper place".
            var expensive = first.MedianPricePerM2 >= second.MedianPricePerM2 ? first : second;
            var cheaper = ReferenceEquals(expensive, first) ? second : first;

            if (expensive.MedianPricePerM2 <= 0)
            {
                return null;
            }

            var gapPercent = (expensive.MedianPricePerM2 - cheaper.MedianPricePerM2) / expensive.MedianPricePerM2 * 100m;

            if (gapPercent < request.MinGapPercent)
            {
                return null;
            }

            return new NeighbourGapResponse
            {
                ExpensivePlace = Converter.PlaceName(expensive),
                ExpensivePricePerM2 = expensive.MedianPricePerM2,
                ExpensiveListingCount = expensive.ListingCount,
                CheaperPlace = Converter.PlaceName(cheaper),
                CheaperPricePerM2 = cheaper.MedianPricePerM2,
                CheaperListingCount = cheaper.ListingCount,
                DistanceKm = decimal.Round(distanceKm, 1),
                GapPercent = decimal.Round(gapPercent, 1)
            };
        }

        /// <summary>
        /// When these rows were worked out. Every row of one run carries the same stamp, so the
        /// first one speaks for all of them.
        /// </summary>
        private static DateTime? LastCalculatedAt(List<MarketAreaStats> rows)
        {
            return rows.Count == 0 ? null : rows[0].CalculatedAtUtc;
        }
    }
}
