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
            var rows = await _marketAreaStatsRepo.GetLeaderboardAsync(
                request.Level, request.MinListings, request.District, request.Municipality);

            return new MarketAreaLeaderboardResponse
            {
                Items = rows.Select(Converter.MapToResponse).ToList(),
                CalculatedAtUtc = LastCalculatedAt(rows)
            };
        }

        /// <inheritdoc/>
        public async Task<MarketAreaBudgetResponse> GetBudgetRankingAsync(MarketAreaBudgetRequest request)
        {
            var rows = await _marketAreaStatsRepo.GetWithTypologiesAsync(
                request.Level, request.MinListings, request.District, request.Municipality);

            // What the budget is allowed to reach once stretched. Zero stretch leaves it alone,
            // which is the default: nothing over budget is affordable unless you say so.
            var reach = request.Budget * (1m + request.StretchPercent / 100m);

            var response = new MarketAreaBudgetResponse
            {
                Budget = request.Budget,
                Reach = reach,
                CalculatedAtUtc = LastCalculatedAt(rows)
            };

            foreach (var place in rows)
            {
                var affordable = BestWithinBudget(place, reach);

                // No typology here usually sells for the budget, so the place is left out
                // entirely rather than listed with an empty answer.
                if (affordable is null)
                {
                    continue;
                }

                // Asked for a T3 and the budget only reaches a T1 here: that is not an answer to
                // the question, so the place goes rather than being listed as a near miss.
                if (request.MinTypology is not null && affordable.Typology < request.MinTypology)
                {
                    continue;
                }

                var item = Converter.MapToBudgetItem(place, affordable);

                // Reached only because the budget was stretched. Flagged rather than hidden or
                // silently mixed in - "you could have this for 8% more" is worth knowing, and
                // worth knowing that it is what you are looking at.
                item.NeedsStretch = affordable.MedianPrice > request.Budget;

                item.AffordableTypologies = place.TypologyStats
                    .Where(x => x.MedianPrice <= reach)
                    .OrderByDescending(x => x.Typology)
                    .Select(Converter.MapToBudgetTypology)
                    .ToList();

                response.Items.Add(item);
            }

            return response;
        }

        /// <inheritdoc/>
        public async Task<MarketAreaNeighbourGapResponse> GetNeighbourGapsAsync(MarketAreaNeighbourGapRequest request)
        {
            // The typology children only come along when a typology was asked for: comparing
            // like with like needs them, comparing all stock does not, and they are several
            // thousand rows.
            var rows = request.Typology is null
                ? await _marketAreaStatsRepo.GetLeaderboardAsync(
                    request.Level, request.MinListings, request.District, request.Municipality)
                : await _marketAreaStatsRepo.GetWithTypologiesAsync(
                    request.Level, request.MinListings, request.District, request.Municipality);

            // A place with no coordinates cannot be anybody's neighbour.
            var placeable = rows
                .Where(x => x.CentroidLatitude.HasValue && x.CentroidLongitude.HasValue)
                .ToList();

            var response = new MarketAreaNeighbourGapResponse
            {
                ComparedOn = request.Typology,
                CalculatedAtUtc = LastCalculatedAt(rows)
            };

            // Reduced to "the place, and the one price we are comparing it on" before any pairing
            // happens, so the pairwise loop below never has to know whether a typology was asked
            // for. Places that cannot be compared on that basis drop out here, once, rather than
            // being re-checked against every other place.
            var comparable = placeable
                .Select(x => ToComparable(x, request))
                .Where(x => x is not null)
                .Select(x => x!.Value)
                .ToList();

            // Each pair once: the inner loop starts past the outer one, so A/B is compared but
            // B/A is not, and nothing is compared with itself.
            for (var first = 0; first < comparable.Count; first++)
            {
                for (var second = first + 1; second < comparable.Count; second++)
                {
                    var gap = BuildGap(comparable[first], comparable[second], request);

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

        /// <inheritdoc/>
        public async Task<TopDealsResponse> GetTopDealsAsync(TopDealsRequest request)
        {
            // Same sample gate as the leaderboard - a median from a handful of listings is not
            // a market to grade a deal against. With typology children, because a T5 house and a
            // T1 apartment do not sell for the same €/m² even in the same town.
            var townStats = await _marketAreaStatsRepo.GetWithTypologiesAsync(
                AreaLevel.Town, minListings: 5, request.District, request.Municipality);

            var statsByTown = townStats.ToDictionary(x => (x.District, x.Municipality, x.Town), x => x);

            var listings = await _propertyListingRepo.GetActiveListingsForTopDealsAsync(
                request.District, request.Municipality, request.Town, request.Zone, request.Condition);

            var deals = new List<TopDealResponse>();

            foreach (var listing in listings)
            {
                var snapshot = listing.ListingSnapshots.FirstOrDefault();

                if (snapshot is null || snapshot.PricePerM2 <= 0)
                {
                    continue;
                }

                var key = (listing.MarketArea.District, listing.MarketArea.Municipality, listing.MarketArea.Town);

                // No trustworthy stats for this listing's town -> nothing to grade it against.
                if (!statsByTown.TryGetValue(key, out var stats))
                {
                    continue;
                }

                // A T5 house legitimately costs less per m² than a typical apartment in the same
                // town - grading it against a town-wide blended median would manufacture a "deal"
                // that is really just a size effect. Each listing is graded against its own
                // typology's median instead. A typology with too few listings for a median (see
                // MarketAreaStatsCalculator's own minimum) has no fair basis and drops out.
                var typologyStats = stats.TypologyStats.FirstOrDefault(x => x.Typology == listing.Typology);

                if (typologyStats is null || typologyStats.MedianPricePerM2 <= 0)
                {
                    continue;
                }

                // A typology can still span very different sizes in one town (a "T5" might be a
                // modest house or a villa twice the size) - comparing a listing far outside its
                // own typology's usual size here would reproduce the same size distortion
                // typology banding exists to avoid. Same +-25% band
                // GetComparablePropertyListingAsync uses for a fair comp.
                const decimal areaBand = 0.25m;
                var isTypicalSizeForTypology = listing.AreaM2 >= typologyStats.MedianAreaM2 * (1 - areaBand)
                    && listing.AreaM2 <= typologyStats.MedianAreaM2 * (1 + areaBand);

                if (!isTypicalSizeForTypology)
                {
                    continue;
                }

                var medianPricePerM2 = typologyStats.MedianPricePerM2;

                var discountPercent = (medianPricePerM2 - snapshot.PricePerM2) / medianPricePerM2 * 100m;

                // Asking at or above the median is not a deal.
                if (discountPercent <= 0)
                {
                    continue;
                }

                deals.Add(new TopDealResponse
                {
                    Listing = Converter.MapToResponse(listing, snapshot),
                    TownMedianPricePerM2 = medianPricePerM2,
                    DiscountPercent = decimal.Round(discountPercent, 1)
                });
            }

            return new TopDealsResponse
            {
                Items = deals.OrderByDescending(x => x.DiscountPercent).Take(request.Count).ToList(),
                CalculatedAtUtc = LastCalculatedAt(townStats)
            };
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
        /// One place reduced to the single price the comparison will run on, or null when it
        /// cannot be compared on that basis at all.
        ///
        /// With no typology asked for that price is the place's overall median, and every place
        /// qualifies. With one, it is that typology's median here, and a place without enough of
        /// that typology drops out - a "T4 gap" measured off three adverts is exactly the finding
        /// this screen should not produce.
        /// </summary>
        private static ComparablePlace? ToComparable(MarketAreaStats place, MarketAreaNeighbourGapRequest request)
        {
            if (request.Typology is null)
            {
                return new ComparablePlace(place, place.MedianPricePerM2, place.ListingCount);
            }

            var forTypology = place.TypologyStats.FirstOrDefault(x => x.Typology == request.Typology);

            if (forTypology is null || forTypology.ListingCount < request.MinTypologyListings)
            {
                return null;
            }

            return new ComparablePlace(place, forTypology.MedianPricePerM2, forTypology.ListingCount);
        }

        /// <summary>
        /// One pair of places as a gap, or null when they are too far apart, too alike in price,
        /// or a pair we could not measure.
        /// </summary>
        private static NeighbourGapResponse? BuildGap(
            ComparablePlace first, ComparablePlace second, MarketAreaNeighbourGapRequest request)
        {
            var distanceMeters = Calculator.CalculateDistanceMeters(
                (double)first.Stats.CentroidLatitude!.Value,
                (double)first.Stats.CentroidLongitude!.Value,
                (double)second.Stats.CentroidLatitude!.Value,
                (double)second.Stats.CentroidLongitude!.Value);

            var distanceKm = (decimal)(distanceMeters / 1000);

            if (distanceKm > request.MaxDistanceKm)
            {
                return null;
            }

            // Sort the pair by price so the response always reads "dear place -> cheaper place".
            var expensive = first.PricePerM2 >= second.PricePerM2 ? first : second;
            var cheaper = ReferenceEquals(expensive.Stats, first.Stats) ? second : first;

            if (expensive.PricePerM2 <= 0)
            {
                return null;
            }

            var gapPercent = (expensive.PricePerM2 - cheaper.PricePerM2) / expensive.PricePerM2 * 100m;

            if (gapPercent < request.MinGapPercent)
            {
                return null;
            }

            return new NeighbourGapResponse
            {
                Expensive = Converter.MapToGapPlace(expensive.Stats, expensive.PricePerM2, expensive.ListingCount),
                Cheaper = Converter.MapToGapPlace(cheaper.Stats, cheaper.PricePerM2, cheaper.ListingCount),
                DistanceKm = decimal.Round(distanceKm, 1),
                GapPercent = decimal.Round(gapPercent, 1)
            };
        }

        /// <summary>
        /// A place and the one price the comparison is running on, with the count that price
        /// rests on. Keeps "which basis are we comparing on" out of the pairwise loop entirely.
        /// </summary>
        private readonly record struct ComparablePlace(MarketAreaStats Stats, decimal PricePerM2, int ListingCount);

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
