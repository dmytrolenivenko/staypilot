using StayPilot.Domain.Entities;

namespace StayPilot.Application.Helpers.Calculators
{
    /// <summary>
    /// Projects one property forward, from two growth rates blended together:
    ///
    ///   - The seeded rate for its district. A long run planning assumption, not a measurement,
    ///     which is exactly why it is here: it carries the decades this database has not seen.
    ///   - The local rate measured from the snapshots around it. Real, current, and far too short
    ///     a series to extrapolate ten years from on its own.
    ///
    /// Neither is trusted alone. The local rate earns weight in proportion to how long the series
    /// behind it actually runs, and is capped at half the blend however long that gets - a market
    /// that ran hot for a year does not run hot for a decade, and a database that has watched one
    /// summer should not be allowed to say it does.
    ///
    /// Both halves come back separately, so a projection can always be taken apart on screen.
    /// </summary>
    public static class GrowthForecastCalculator
    {
        /// <summary>Days of local series that would earn the local rate its full share.</summary>
        private const decimal DaysForFullLocalWeight = 730m;

        /// <summary>The most say the local trend ever gets, however long the series.</summary>
        private const decimal MaximumLocalWeight = 0.5m;

        /// <summary>Below this many snapshots there is no local trend worth fitting.</summary>
        public const int MinimumSnapshotsForTrend = 30;

        /// <summary>Below this many days apart, two points are noise rather than a slope.</summary>
        public const int MinimumTrendSpanDays = 60;

        /// <summary>
        /// A short series can produce a slope of any size at all. Beyond this the number stops
        /// being a trend and starts being an artefact, so it is held here and said to be held.
        /// </summary>
        public const decimal MaximumLocalAnnualPercent = 25m;

        /// <summary>What the local snapshots say about which way prices are going.</summary>
        public readonly record struct LocalTrend(
            decimal? AnnualPercent,
            bool WasCapped,
            int SnapshotCount,
            int SpanDays,
            int MonthsObserved,
            string Reason);

        /// <summary>One projected path: a rate, and what the property is worth along it.</summary>
        public readonly record struct GrowthScenario(string Name, decimal AnnualPercent, IReadOnlyList<decimal> Values);

        /// <summary>The whole forecast, with both halves still visible.</summary>
        public readonly record struct Forecast(
            decimal SeededAnnualPercent,
            string SeededSource,
            decimal? LocalAnnualPercent,
            decimal LocalWeightPercent,
            decimal BlendedAnnualPercent,
            LocalTrend Trend,
            IReadOnlyList<GrowthScenario> Scenarios);

        /// <summary>
        /// Measures which way asking prices moved in one place, as a percent per year.
        ///
        /// Fitted on the median price per square metre of each month rather than on the raw
        /// snapshots: without the median, a month in which one expensive block was advertised
        /// forty times becomes a price rise. Months are the grain because a week of adverts in a
        /// small town is a handful of homes.
        /// </summary>
        public static LocalTrend MeasureLocalTrend(IReadOnlyList<PropertyListing> listings, DateTime asOfUtc)
        {
            var snapshots = listings
                .SelectMany(x => x.ListingSnapshots)
                .Where(x => x.PricePerM2 > 0m)
                .ToList();

            if (snapshots.Count < MinimumSnapshotsForTrend)
            {
                return new LocalTrend(null, false, snapshots.Count, 0, 0,
                    $"{snapshots.Count} price observations here, and a trend needs at least {MinimumSnapshotsForTrend}");
            }

            var earliest = snapshots.Min(x => x.SnapshotDateUtc);
            var spanDays = (int)Math.Round((snapshots.Max(x => x.SnapshotDateUtc) - earliest).TotalDays);

            if (spanDays < MinimumTrendSpanDays)
            {
                return new LocalTrend(null, false, snapshots.Count, spanDays, 0,
                    $"all {snapshots.Count} price observations fall inside {spanDays} days, which is too short to have a direction");
            }

            // One point per month: the month, and the middle price per m2 advertised in it.
            var monthly = snapshots
                .GroupBy(x => new DateTime(x.SnapshotDateUtc.Year, x.SnapshotDateUtc.Month, 1))
                .OrderBy(x => x.Key)
                .Select(x => (
                    MonthIndex: (decimal)((x.Key.Year * 12) + x.Key.Month),
                    Median: Calculator.Median(x.Select(s => s.PricePerM2).OrderBy(s => s).ToList())))
                .ToList();

            if (monthly.Count < 3)
            {
                return new LocalTrend(null, false, snapshots.Count, spanDays, monthly.Count,
                    $"prices land in only {monthly.Count} months, and a trend needs at least 3");
            }

            // Straight line through the monthly medians. The slope is euros per m2 per month;
            // twelve of those over the average level is the annual rate.
            var firstMonth = monthly[0].MonthIndex;

            var slopePerMonth = LeastSquaresSlope(
                monthly.Select(x => x.MonthIndex - firstMonth).ToList(),
                monthly.Select(x => x.Median).ToList());

            var averageLevel = monthly.Average(x => x.Median);

            if (averageLevel <= 0m)
            {
                return new LocalTrend(null, false, snapshots.Count, spanDays, monthly.Count,
                    "the prices behind the trend average to nothing, so there is no rate to take");
            }

            var annual = slopePerMonth * 12m / averageLevel * 100m;

            var capped = Math.Abs(annual) > MaximumLocalAnnualPercent;

            var held = capped
                ? Math.Sign(annual) * MaximumLocalAnnualPercent
                : Math.Round(annual, 1);

            var reason = capped
                ? $"prices here moved {annual:0}% a year over {monthly.Count} months, held at {MaximumLocalAnnualPercent:0}% because a series this short cannot support more"
                : $"prices here moved {held:0.0}% a year over {monthly.Count} months of adverts";

            return new LocalTrend(held, capped, snapshots.Count, spanDays, monthly.Count, reason);
        }

        /// <summary>
        /// Blends the seeded and local rates and walks the value forward year by year.
        ///
        /// Three paths come back rather than one. A single number invites being read as a
        /// prediction; three make the width of the uncertainty the first thing on screen. The
        /// width is the district's own volatility, so a tourist market fans wider than a quiet one.
        /// </summary>
        public static Forecast Calculate(decimal currentValue, HousePriceGrowth seeded, LocalTrend trend, int years)
        {
            // Weight grows with the length of the series and stops at half. Zero when there is no
            // local rate at all, which leaves the seeded figure carrying the whole forecast.
            var localWeight = trend.AnnualPercent is null
                ? 0m
                : Math.Min(MaximumLocalWeight, trend.SpanDays / DaysForFullLocalWeight);

            var blended = (seeded.AnnualGrowthPercent * (1m - localWeight)) + ((trend.AnnualPercent ?? 0m) * localWeight);

            blended = Math.Round(blended, 2);

            var swing = seeded.VolatilityPercentagePoints;

            var scenarios = new List<GrowthScenario>
            {
                new GrowthScenario("Conservative", Math.Round(blended - swing, 2), Path(currentValue, blended - swing, years)),
                new GrowthScenario("Base", blended, Path(currentValue, blended, years)),
                new GrowthScenario("Optimistic", Math.Round(blended + swing, 2), Path(currentValue, blended + swing, years)),
            };

            return new Forecast(
                seeded.AnnualGrowthPercent,
                seeded.Source,
                trend.AnnualPercent,
                Math.Round(localWeight * 100m, 1),
                blended,
                trend,
                scenarios);
        }

        /// <summary>
        /// The value at the end of each year, index 0 being today. Compounded, because a rate per
        /// year applied ten times is not ten times the rate.
        /// </summary>
        private static List<decimal> Path(decimal currentValue, decimal annualPercent, int years)
        {
            var values = new List<decimal>(years + 1);

            var factor = 1m + (annualPercent / 100m);

            var value = currentValue;

            values.Add(Math.Round(value, 0));

            for (var year = 1; year <= years; year++)
            {
                value *= factor;

                // A rate below -100% would flip the sign and walk the value up again. Nothing in
                // the seed goes near it, but a projection that turns negative is nonsense whatever
                // put it there.
                values.Add(Math.Round(Math.Max(value, 0m), 0));
            }

            return values;
        }

        /// <summary>
        /// Slope of the least-squares line through the points. Kept here rather than reaching for
        /// LeastSquares, which fits the valuation model's many-variable design matrix - this is
        /// two columns and does not need it.
        /// </summary>
        private static decimal LeastSquaresSlope(IReadOnlyList<decimal> xs, IReadOnlyList<decimal> ys)
        {
            var meanX = xs.Average();
            var meanY = ys.Average();

            var covariance = 0m;
            var variance = 0m;

            for (var i = 0; i < xs.Count; i++)
            {
                var dx = xs[i] - meanX;

                covariance += dx * (ys[i] - meanY);
                variance += dx * dx;
            }

            return variance == 0m ? 0m : covariance / variance;
        }
    }
}
