using StayPilot.Application.Contracts.Response;
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Helpers.Calculators
{
    /// <summary>
    /// Turns one slice of listings into the numbers the market overview screen shows: the four
    /// summaries, the price distribution, and a row per room layout.
    ///
    /// Nothing is saved. This runs on every request, because the slice is whatever the caller
    /// asked for and no precomputed table can hold every town crossed with every layout.
    /// </summary>
    public static class MarketOverviewCalculator
    {
        /// <summary>
        /// Where the distribution's bars start and stop. The outer 5% at each end is left out of
        /// the bar WIDTHS - not out of the counts - because raw min/max let one four-million villa
        /// stretch the range until nine bars are empty and the tenth holds the whole town. The
        /// listings past the edges are still counted, in the first and the last bar.
        /// </summary>
        private const double DistributionLowPercentile = 0.05;

        /// <inheritdoc cref="DistributionLowPercentile"/>
        private const double DistributionHighPercentile = 0.95;

        /// <summary>
        /// The fewest and the most bars we will draw, whatever the caller asks for. The request
        /// carries the same range as a validation attribute; this is the belt to that pair of
        /// braces, so a bad number cannot divide by zero in here.
        /// </summary>
        private const int MinimumBuckets = 4;

        /// <inheritdoc cref="MinimumBuckets"/>
        private const int MaximumBuckets = 20;

        /// <summary>
        /// Works the overview out from these listings. Listings with no newest snapshot, no price
        /// or no area are dropped: they cannot be measured, and counting them would make the
        /// listing count promise more evidence than there is.
        /// </summary>
        public static MarketOverviewResponse Calculate(
            IEnumerable<PropertyListing> listings, int bucketCount, AreaLevel? breakdownLevel = null)
        {
            var measurable = CollectMeasurable(listings);

            var response = new MarketOverviewResponse
            {
                ListingCount = measurable.Count,
                GeneratedAtUtc = DateTime.UtcNow
            };

            // Nothing here matches what was asked for. An empty answer, not an error - the screen
            // says "no listings", which is itself information about the slice.
            if (measurable.Count == 0)
            {
                return response;
            }

            var prices = Sorted(measurable.Select(x => x.Price));

            response.Price = Summarise(prices);
            response.PricePerM2 = Summarise(Sorted(measurable.Select(x => x.PricePerM2)));
            response.AreaM2 = Summarise(Sorted(measurable.Select(x => (decimal)x.AreaM2)));
            response.Distribution = BuildDistribution(prices, bucketCount);
            response.Typologies = BuildTypologyRows(measurable);
            response.Breakdown = BuildBreakdown(measurable, breakdownLevel, response.PricePerM2.Median);

            return response;
        }

        /// <summary>
        /// The listings we can measure, cut down to the four numbers the screen needs. Cut down
        /// here so the maths below never has to reach back into an entity or its snapshots.
        /// </summary>
        private static List<MeasuredListing> CollectMeasurable(IEnumerable<PropertyListing> listings)
        {
            var measurable = new List<MeasuredListing>();

            foreach (var listing in listings)
            {
                var snapshot = NewestSnapshot(listing);

                if (snapshot is null || snapshot.Price <= 0 || snapshot.PricePerM2 <= 0 || listing.AreaM2 <= 0)
                {
                    continue;
                }

                var area = listing.MarketArea;

                measurable.Add(new MeasuredListing(
                    snapshot.Price,
                    snapshot.PricePerM2,
                    listing.AreaM2,
                    listing.Typology,
                    area?.District ?? string.Empty,
                    area?.Municipality ?? string.Empty,
                    area?.Town ?? string.Empty));
            }

            return measurable;
        }

        /// <summary>
        /// One quantity, summarised four ways. The list must already be sorted ascending, which is
        /// what makes the min, the max and the median a lookup instead of three more passes.
        /// </summary>
        private static MarketOverviewStats Summarise(List<decimal> sortedAscending)
        {
            return new MarketOverviewStats
            {
                Median = decimal.Round(Calculator.Median(sortedAscending), 2),
                Average = decimal.Round(sortedAscending.Average(), 2),
                Min = decimal.Round(sortedAscending[0], 2),
                Max = decimal.Round(sortedAscending[^1], 2)
            };
        }

        /// <summary>
        /// The price distribution: equal-width bars between the 5th and the 95th percentile, with
        /// everything cheaper counted into the first bar and everything dearer into the last.
        ///
        /// The two edge bars report the real cheapest and dearest price rather than the percentile,
        /// so the range printed on the first and last bar still matches the min and max above it.
        /// </summary>
        private static List<MarketOverviewPriceBucketResponse> BuildDistribution(List<decimal> sortedPrices, int bucketCount)
        {
            var bars = Math.Clamp(bucketCount, MinimumBuckets, MaximumBuckets);

            var cheapest = sortedPrices[0];
            var dearest = sortedPrices[^1];
            var low = Calculator.Percentile(sortedPrices, DistributionLowPercentile);
            var high = Calculator.Percentile(sortedPrices, DistributionHighPercentile);

            // Everything asks the same, or near enough that the middle 90% is one single price.
            // One bar is then the honest picture; cutting it into ten would draw nine empty ones.
            if (high <= low)
            {
                return new List<MarketOverviewPriceBucketResponse>
                {
                    Bucket(cheapest, dearest, sortedPrices.Count, sortedPrices.Count)
                };
            }

            var width = (high - low) / bars;
            var counts = new int[bars];

            foreach (var price in sortedPrices)
            {
                var bar = (int)decimal.Floor((price - low) / width);

                // Below the 5th or above the 95th percentile: counted at the near end rather than
                // dropped. The bar widths ignore the outliers, the counts never do.
                counts[Math.Clamp(bar, 0, bars - 1)]++;
            }

            var buckets = new List<MarketOverviewPriceBucketResponse>();

            for (var bar = 0; bar < bars; bar++)
            {
                var from = bar == 0 ? cheapest : low + bar * width;
                var to = bar == bars - 1 ? dearest : low + (bar + 1) * width;

                buckets.Add(Bucket(from, to, counts[bar], sortedPrices.Count));
            }

            return buckets;
        }

        /// <summary>
        /// One row per room layout present, fewest rooms first. Layouts with only one or two
        /// listings are kept rather than gated away as they are on the leaderboard: the count sits
        /// in its own column on this screen, so a thin row reads as thin, and "there are two T5s
        /// here at all" is part of what an overview is for.
        /// </summary>
        private static List<MarketOverviewTypologyResponse> BuildTypologyRows(List<MeasuredListing> measurable)
        {
            var rows = new List<MarketOverviewTypologyResponse>();

            foreach (var group in measurable.GroupBy(x => x.Typology).OrderBy(x => x.Key))
            {
                var forTypology = group.ToList();

                rows.Add(new MarketOverviewTypologyResponse
                {
                    Typology = group.Key,
                    ListingCount = forTypology.Count,
                    MedianPrice = decimal.Round(Calculator.Median(Sorted(forTypology.Select(x => x.Price))), 2),
                    MedianAreaM2 = decimal.Round(Calculator.Median(Sorted(forTypology.Select(x => (decimal)x.AreaM2))), 2),
                    MedianPricePerM2 = decimal.Round(Calculator.Median(Sorted(forTypology.Select(x => x.PricePerM2))), 2)
                });
            }

            return rows;
        }

        /// <summary>
        /// The slice cut into the places inside it, one row each, dearest per square meter first.
        ///
        /// Null when there is no finer grain to cut to (the slice is already one freguesia), which
        /// is the caller's decision, not ours - it knows what was narrowed.
        ///
        /// Places whose listings carry no market area are dropped rather than collected under a
        /// blank name: a row called "" is not a place, and its median would be read as one.
        /// </summary>
        private static MarketOverviewBreakdownResponse? BuildBreakdown(
            List<MeasuredListing> measurable, AreaLevel? level, decimal slicePricePerM2)
        {
            if (level is null)
            {
                return null;
            }

            var breakdown = new MarketOverviewBreakdownResponse { Level = level.Value };

            var placed = measurable.Where(x => !string.IsNullOrWhiteSpace(PlaceKeyFor(x, level.Value)));

            foreach (var group in placed.GroupBy(x => PlaceKeyFor(x, level.Value)))
            {
                var inPlace = group.ToList();
                var first = inPlace[0];
                var pricePerM2 = decimal.Round(Calculator.Median(Sorted(inPlace.Select(x => x.PricePerM2))), 2);

                breakdown.Items.Add(new MarketOverviewBreakdownItemResponse
                {
                    District = first.District,
                    Municipality = level.Value == AreaLevel.District ? string.Empty : first.Municipality,
                    Town = level.Value == AreaLevel.Town ? first.Town : string.Empty,
                    DisplayName = group.Key,
                    ListingCount = inPlace.Count,
                    SharePercent = decimal.Round((decimal)inPlace.Count / measurable.Count * 100m, 1),
                    MedianPrice = decimal.Round(Calculator.Median(Sorted(inPlace.Select(x => x.Price))), 2),
                    MedianAreaM2 = decimal.Round(Calculator.Median(Sorted(inPlace.Select(x => (decimal)x.AreaM2))), 2),
                    MedianPricePerM2 = pricePerM2,

                    // Against the slice this place belongs to, not against the country. Zero when
                    // the slice itself has no price to compare with.
                    VsSlicePercent = slicePricePerM2 <= 0
                        ? 0m
                        : decimal.Round((pricePerM2 - slicePricePerM2) / slicePricePerM2 * 100m, 1)
                });
            }

            breakdown.Items = breakdown.Items
                .OrderByDescending(x => x.MedianPricePerM2)
                .ToList();

            return breakdown;
        }

        /// <summary>
        /// Which place one listing counts into at this grain. Also the row's display name, so the
        /// grouping key and the label can never drift apart.
        /// </summary>
        private static string PlaceKeyFor(MeasuredListing listing, AreaLevel level)
        {
            return level switch
            {
                AreaLevel.District => listing.District,
                AreaLevel.Municipality => listing.Municipality,
                _ => listing.Town
            };
        }

        private static MarketOverviewPriceBucketResponse Bucket(decimal from, decimal to, int listingCount, int totalListings)
        {
            return new MarketOverviewPriceBucketResponse
            {
                FromPrice = decimal.Round(from, 2),
                ToPrice = decimal.Round(to, 2),
                ListingCount = listingCount,
                SharePercent = decimal.Round((decimal)listingCount / totalListings * 100m, 1)
            };
        }

        /// <summary>
        /// The newest snapshot on this listing, or null when it has none. The newest one is the
        /// current asking price; the older ones are the price history screen's business.
        /// </summary>
        private static ListingSnapshot? NewestSnapshot(PropertyListing listing)
        {
            return listing.ListingSnapshots
                .OrderByDescending(x => x.SnapshotDateUtc)
                .FirstOrDefault();
        }

        private static List<decimal> Sorted(IEnumerable<decimal> values)
        {
            var sorted = values.ToList();
            sorted.Sort();

            return sorted;
        }

        /// <summary>
        /// One listing cut down to what the overview measures. A record, so the grouping and the
        /// medians above read as plain maths over plain values.
        /// </summary>
        private readonly record struct MeasuredListing(
            decimal Price,
            decimal PricePerM2,
            int AreaM2,
            Typology Typology,
            string District,
            string Municipality,
            string Town);
    }
}
