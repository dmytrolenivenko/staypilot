using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Helpers.Calculators
{
    /// <summary>
    /// Scores how keen buyers are in one place, out of a hundred, from two things only:
    ///
    ///   1. How long homes sit before their last sighting - the faster they go, the keener.
    ///   2. Whether new adverts are arriving faster than they were - a pile-up of fresh stock is
    ///      supply outrunning demand, a thinning one is the opposite.
    ///
    /// Both come from adverts, so this is interest in a place and not sales. Anything it cannot
    /// measure it reports as not measured, never as the middle of the scale: a place with no
    /// evidence and a place that is genuinely average must not read the same.
    ///
    /// The honest limit is that CreatedAtUtc is when the scraper first saw a listing, not when it
    /// went up. Early on that makes every listing look young simply because collection is young,
    /// so the days-on-market half switches itself off until the collection window is comfortably
    /// wider than the number it is measuring.
    /// </summary>
    public static class DemandCalculator
    {
        /// <summary>Below this many listings the place is not worth scoring at all.</summary>
        public const int MinimumListings = 10;

        /// <summary>The two supply windows compared against each other, in days.</summary>
        public const int SupplyWindowDays = 90;

        /// <summary>How many sold listings it takes before we prefer them over live ones.</summary>
        public const int MinimumSoldListings = 8;

        /// <summary>Sitting this long or less is as fast as the scale goes.</summary>
        private const decimal FastestDays = 30m;

        /// <summary>Sitting this long or more is as slow as the scale goes.</summary>
        private const decimal SlowestDays = 180m;

        /// <summary>
        /// Supply moving this far in either direction over one window pins the supply half of the
        /// score. Half as much stock again is a glut; a third fewer adverts is a drought.
        /// </summary>
        private const decimal SupplySwingPercent = 50m;

        /// <summary>
        /// A measured median only counts while it stays well inside the collection window. At 0.6
        /// a place whose homes "sit 100 days" needs 167 days of collection behind it before we
        /// believe the 100 - otherwise we are measuring our own start date. The check is run
        /// against at least <see cref="FastestDays"/> even when the actual median is lower (or
        /// zero): a young collection window makes every listing look brand new regardless of what
        /// the true median is, so a suspiciously fast reading needs exactly as much collection
        /// history behind it as a merely fast one would.
        /// </summary>
        private const decimal UsableShareOfCollectionWindow = 0.6m;

        /// <summary>What the scoring found for one place.</summary>
        public readonly record struct DemandOutcome(
            DemandLevel Level,
            decimal Score,
            bool IsMeasurable,
            decimal? MedianDaysOnMarket,
            bool DaysMeasuredOnSold,
            decimal? DaysScore,
            int NewListingsRecent,
            int NewListingsPrevious,
            decimal? SupplyChangePercent,
            decimal? SupplyScore,
            int SampleSize,
            int CollectionSpanDays,
            string Reason);

        /// <summary>
        /// Scores one place. Pass every listing in it, with snapshots loaded.
        /// </summary>
        public static DemandOutcome Calculate(IReadOnlyList<PropertyListing> listings, DateTime asOfUtc)
        {
            var usable = listings
                .Where(x => x.ListingSnapshots.Count > 0)
                .ToList();

            if (usable.Count < MinimumListings)
            {
                return NotMeasured(usable.Count, $"only {usable.Count} listings here, and demand needs at least {MinimumListings}");
            }

            // How long we have been watching this place at all. Every measurement below is
            // bounded by it, so it is worth knowing before either half is believed.
            var collectionSpanDays = (int)Math.Round((asOfUtc - usable.Min(x => x.CreatedAtUtc)).TotalDays);

            var days = ScoreDaysOnMarket(usable, asOfUtc, collectionSpanDays);
            var supply = ScoreSupply(usable, asOfUtc, collectionSpanDays);

            var parts = new List<decimal>();

            if (days.Score is not null)
            {
                parts.Add(days.Score.Value);
            }

            if (supply.Score is not null)
            {
                parts.Add(supply.Score.Value);
            }

            // Neither half could be measured. Say that rather than settling on Balanced, which
            // would read as a finding.
            if (parts.Count == 0)
            {
                return new DemandOutcome(DemandLevel.Balanced, 0m, false, days.MedianDays, days.OnSold, null,
                    supply.Recent, supply.Previous, supply.ChangePercent, null, usable.Count, collectionSpanDays,
                    $"{days.Reason}; {supply.Reason}");
            }

            var score = parts.Sum() / parts.Count;

            var reason = parts.Count == 2
                ? $"{days.Reason}; {supply.Reason}"
                : $"{days.Reason}; {supply.Reason} - scored on the other half alone";

            return new DemandOutcome(Band(score), Math.Round(score, 1), true, days.MedianDays, days.OnSold, days.Score,
                supply.Recent, supply.Previous, supply.ChangePercent, supply.Score, usable.Count, collectionSpanDays, reason);
        }

        /// <summary>
        /// The first half: how long homes sit. Sold listings answer it properly - first sighting
        /// to the day it went - and live ones only approximate it with how long they have been up
        /// so far, so sold is preferred whenever there are enough of them.
        /// </summary>
        private static DaysOutcome ScoreDaysOnMarket(IReadOnlyList<PropertyListing> listings, DateTime asOfUtc, int collectionSpanDays)
        {
            var sold = new List<decimal>();
            var live = new List<decimal>();

            foreach (var listing in listings)
            {
                var newest = listing.ListingSnapshots.OrderByDescending(x => x.SnapshotDateUtc).First();

                if (newest.Status == ListingStatus.Sold)
                {
                    sold.Add((decimal)(newest.SnapshotDateUtc - listing.CreatedAtUtc).TotalDays);

                    continue;
                }

                live.Add((decimal)(asOfUtc - listing.CreatedAtUtc).TotalDays);
            }

            var onSold = sold.Count >= MinimumSoldListings;
            var sample = onSold ? sold : live;

            if (sample.Count == 0)
            {
                return new DaysOutcome(null, onSold, null, "nothing to measure how long homes sit");
            }

            var median = Calculator.Median(sample.OrderBy(x => x).ToList());

            var basis = onSold
                ? $"homes that sold sat {median:0} days"
                : $"homes still up have been up {median:0} days";

            // The measurement has run into the edge of what we have collected. Reporting it would
            // be reporting our own start date dressed up as a market fact. Floored at FastestDays
            // so a median of 0 (or anything below it) can't slip past this guard just because it
            // is small - a window too young to trust "30 days" is too young to trust "0" as well.
            if (Math.Max(median, FastestDays) > collectionSpanDays * UsableShareOfCollectionWindow)
            {
                return new DaysOutcome(median, onSold, null,
                    $"{basis}, too close to the {collectionSpanDays} days of history we hold to mean anything yet");
            }

            var placed = Math.Clamp((SlowestDays - median) / (SlowestDays - FastestDays), 0m, 1m);

            return new DaysOutcome(median, onSold, placed * 100m, basis);
        }

        /// <summary>
        /// The second half: whether fresh adverts are arriving faster than they were. Counted on
        /// first sighting, comparing the last window against the one before it.
        /// </summary>
        private static SupplyOutcome ScoreSupply(IReadOnlyList<PropertyListing> listings, DateTime asOfUtc, int collectionSpanDays)
        {
            var recentFrom = asOfUtc.AddDays(-SupplyWindowDays);
            var previousFrom = asOfUtc.AddDays(-SupplyWindowDays * 2);

            var recent = listings.Count(x => x.CreatedAtUtc > recentFrom);
            var previous = listings.Count(x => x.CreatedAtUtc > previousFrom && x.CreatedAtUtc <= recentFrom);

            // Two full windows have to have happened for there to be a before and an after.
            if (collectionSpanDays < SupplyWindowDays * 2)
            {
                return new SupplyOutcome(recent, previous, null, null,
                    $"only {collectionSpanDays} days of history, and comparing supply needs {SupplyWindowDays * 2}");
            }

            if (previous == 0)
            {
                return new SupplyOutcome(recent, previous, null, null,
                    "no listings in the earlier window to compare new supply against");
            }

            var changePercent = ((decimal)recent - previous) / previous * 100m;

            // More new stock than before is supply outrunning demand, so the score runs the other
            // way to the change.
            var placed = Math.Clamp((SupplySwingPercent - changePercent) / (SupplySwingPercent * 2), 0m, 1m);

            var direction = changePercent > 0 ? "more" : "fewer";

            var reason = Math.Abs(changePercent) < 5m
                ? $"new adverts arriving at about the same rate as the previous {SupplyWindowDays} days"
                : $"{Math.Abs(changePercent):0}% {direction} new adverts than the previous {SupplyWindowDays} days";

            return new SupplyOutcome(recent, previous, Math.Round(changePercent, 1), placed * 100m, reason);
        }

        /// <summary>Turns the score into the word shown on screen.</summary>
        public static DemandLevel Band(decimal score)
        {
            if (score < 20m)
            {
                return DemandLevel.Cold;
            }

            if (score < 40m)
            {
                return DemandLevel.Soft;
            }

            if (score < 60m)
            {
                return DemandLevel.Balanced;
            }

            if (score < 80m)
            {
                return DemandLevel.Firm;
            }

            return DemandLevel.Hot;
        }

        private static DemandOutcome NotMeasured(int sampleSize, string reason)
        {
            return new DemandOutcome(DemandLevel.Balanced, 0m, false, null, false, null, 0, 0, null, null,
                sampleSize, 0, reason);
        }

        private readonly record struct DaysOutcome(decimal? MedianDays, bool OnSold, decimal? Score, string Reason);

        private readonly record struct SupplyOutcome(int Recent, int Previous, decimal? ChangePercent, decimal? Score, string Reason);
    }
}
