using Microsoft.EntityFrameworkCore;
using StayPilot.Application.Helpers.Calculators;
using StayPilot.Domain.Entities;
using StayPilot.Infrastructure.Persistence;
using Xunit.Abstractions;

namespace StayPilot.UnitTests
{
    /// <summary>
    /// Measures the valuation against real listings, out of sample. Every other test in this
    /// project builds a synthetic market with the answer baked in, which proves the maths does
    /// what it says - and tells us nothing about whether the answer is any good. This one holds
    /// a fifth of the real data back, fits on the rest, and reports how far off it lands.
    ///
    /// Hits a real database, so it is opt-in: set STAYPILOT_BACKTEST=1 to run it.
    ///   $env:STAYPILOT_BACKTEST=1; dotnet test --filter FullyQualifiedName~ValuationBacktest
    /// </summary>
    public class ValuationBacktest
    {
        private const string ConnectionString =
            "Server=(localdb)\\MSSQLLocalDB;Database=StayPilotCompsDb;Trusted_Connection=True;TrustServerCertificate=True;";

        private const int Folds = 5;

        private readonly ITestOutputHelper _output;

        public ValuationBacktest(ITestOutputHelper output)
        {
            _output = output;
        }

        private static bool Enabled => Environment.GetEnvironmentVariable("STAYPILOT_BACKTEST") == "1";

        [Fact]
        public void CurrentModel_FiveFold_ReportsOutOfSampleError()
        {
            if (!Enabled)
                return;

            var listings = LoadListings();

            _output.WriteLine($"Loaded {listings.Count} listings with a usable newest snapshot.");
            _output.WriteLine("");

            var model = new List<Scored>();
            var compMedian = new List<Scored>();
            var globalMedian = new List<Scored>();

            for (var fold = 0; fold < Folds; fold++)
            {
                var training = listings.Where(x => x.Id % Folds != fold).ToList();
                var holdout = listings.Where(x => x.Id % Folds == fold).ToList();

                var fitted = ValuationModel.Fit(training);

                // Baselines, fitted on exactly the same training split.
                var trainingPoints = training
                    .Select(x => (
                        Subject: ValuationSubject.FromListing(x),
                        PricePerM2: (double)NewestSnapshot(x)!.PricePerM2))
                    .ToList();

                var trainingGlobalMedian = MedianOf(trainingPoints.Select(x => x.PricePerM2));

                foreach (var listing in holdout)
                {
                    var subject = ValuationSubject.FromListing(listing);
                    var actual = (double)NewestSnapshot(listing)!.PricePerM2;

                    var prediction = fitted.PredictPricePerM2(subject);

                    var (low, high) = PropertyValuation.PriceRange(
                        prediction.PricePerM2, prediction.Spread);

                    model.Add(new Scored(actual, (double)prediction.PricePerM2, prediction.LocalComparablesUsed,
                        prediction.NearestComparableMeters, listing)
                    {
                        InBand = actual >= (double)low && actual <= (double)high,
                        BandWidthPercent = prediction.PricePerM2 <= 0
                            ? 0
                            : (double)(high - low) / (double)prediction.PricePerM2 * 100,
                    });

                    compMedian.Add(new Scored(actual, LocalCompMedian(trainingPoints, subject, trainingGlobalMedian),
                        0, 0, listing));

                    globalMedian.Add(new Scored(actual, trainingGlobalMedian, 0, 0, listing));
                }

                _output.WriteLine($"fold {fold}: trained on {fitted.TrainingListings} " +
                                  $"(+{fitted.DiscardedListings} trimmed as outliers), " +
                                  $"held out {holdout.Count}, R2 {fitted.RSquared:F3}, spread {fitted.PredictionSpread:F3}");
            }

            _output.WriteLine("");
            Report("MODEL (hedonic + neighbourhood)", model);
            Report("BASELINE comp median (10 nearest)", compMedian);
            Report("BASELINE global median", globalMedian);

            _output.WriteLine("");
            _output.WriteLine("=== MODEL error by local evidence ===");
            Report("  no neighbours at all", model.Where(x => x.NeighboursUsed == 0).ToList());
            Report("  nearest comp <= 1km", model.Where(x => x.NearestMeters <= 1000).ToList());
            Report("  nearest comp 1-5km", model.Where(x => x.NearestMeters > 1000 && x.NearestMeters <= 5000).ToList());
            Report("  nearest comp > 5km", model.Where(x => x.NearestMeters > 5000 && x.NeighboursUsed > 0).ToList());

            _output.WriteLine("");
            _output.WriteLine("=== MODEL error by price bracket (actual EUR/m2) ===");
            Report("  under 1500", model.Where(x => x.Actual < 1500).ToList());
            Report("  1500-2500", model.Where(x => x.Actual >= 1500 && x.Actual < 2500).ToList());
            Report("  2500-4000", model.Where(x => x.Actual >= 2500 && x.Actual < 4000).ToList());
            Report("  4000+", model.Where(x => x.Actual >= 4000).ToList());

            _output.WriteLine("");
            _output.WriteLine("=== QUOTED PRICE RANGE - does it contain the truth? ===");
            _output.WriteLine($"  contains actual: {model.Count(x => x.InBand) * 100.0 / model.Count:F1}% " +
                              $"(a range nobody can rely on is worse than no range)");
            _output.WriteLine($"  median width:    {MedianOf(model.Select(x => x.BandWidthPercent)):F1}% of the estimate");
            _output.WriteLine($"  widest quoted:   {model.Max(x => x.BandWidthPercent):F1}%");

            _output.WriteLine("");
            _output.WriteLine("=== MODEL bias (is it systematically high or low?) ===");
            var signed = model.Select(x => x.Predicted / x.Actual).OrderBy(x => x).ToList();
            _output.WriteLine($"  median predicted/actual ratio: {MedianOf(signed):F4} (1.0 = unbiased)");
            _output.WriteLine($"  mean   predicted/actual ratio: {signed.Average():F4}");

            _output.WriteLine("");
            _output.WriteLine("=== WORST 15 MISSES ===");

            foreach (var miss in model.OrderByDescending(x => x.AbsolutePercentError).Take(15))
            {
                _output.WriteLine(
                    $"  {miss.AbsolutePercentError,6:F0}%  actual {miss.Actual,7:F0}  predicted {miss.Predicted,7:F0}  " +
                    $"area {miss.Listing.AreaM2,4}  {miss.Listing.Typology}  areaId {miss.Listing.MarketAreaId,5}  " +
                    $"nbrs {miss.NeighboursUsed,2}  {miss.Listing.SourceUrl}");
            }
        }

        /// <summary>
        /// The straightforward alternative the model has to beat: the median price per m² of the
        /// nearest training listings. This is what a person would do by hand.
        /// </summary>
        private static double LocalCompMedian(
            List<(ValuationSubject Subject, double PricePerM2)> training, ValuationSubject subject, double fallback)
        {
            if (!subject.Latitude.HasValue || !subject.Longitude.HasValue)
                return fallback;

            var latitude = (double)subject.Latitude.Value;
            var longitude = (double)subject.Longitude.Value;

            var nearest = training
                .Where(x => x.Subject.Latitude.HasValue && x.Subject.Longitude.HasValue)
                .Select(x => (
                    Distance: Calculator.CalculateDistanceMeters(
                        latitude, longitude, (double)x.Subject.Latitude!.Value, (double)x.Subject.Longitude!.Value),
                    x.PricePerM2))
                .Where(x => x.Distance <= 25_000)
                .OrderBy(x => x.Distance)
                .Take(10)
                .Select(x => x.PricePerM2)
                .ToList();

            return nearest.Count == 0 ? fallback : MedianOf(nearest);
        }

        private void Report(string label, IReadOnlyList<Scored> scores)
        {
            if (scores.Count == 0)
            {
                _output.WriteLine($"{label,-38} (no rows)");
                return;
            }

            var errors = scores.Select(x => x.AbsolutePercentError).OrderBy(x => x).ToList();

            _output.WriteLine(
                $"{label,-38} n={scores.Count,6}  " +
                $"median {MedianOf(errors),5:F1}%  " +
                $"mean {errors.Average(),5:F1}%  " +
                $"within10 {errors.Count(x => x <= 10) * 100.0 / errors.Count,4:F0}%  " +
                $"within20 {errors.Count(x => x <= 20) * 100.0 / errors.Count,4:F0}%  " +
                $"p90 {Percentile(errors, 0.90),6:F1}%");
        }

        private static List<PropertyListing> LoadListings()
        {
            var options = new DbContextOptionsBuilder<StayPilotDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;

            using var context = new StayPilotDbContext(options);

            var listings = context.PropertyListings
                .AsNoTracking()
                .Include(x => x.MarketArea)          // the location fallback needs district/municipality
                .Include(x => x.ListingSnapshots)
                .ToList();

            // The same admission rule the fit uses, so the holdout and the training set are drawn
            // from one population. Scoring against a row the model refuses to learn from would
            // measure the scraper rather than the valuation.
            var usable = listings.Where(x => ListingQuality.IsUsable(x, NewestSnapshot(x))).ToList();

            Console.WriteLine($"admission: {usable.Count} usable, {listings.Count - usable.Count} rejected as broken data");

            return usable;
        }

        private static ListingSnapshot? NewestSnapshot(PropertyListing listing)
        {
            return listing.ListingSnapshots.OrderByDescending(x => x.SnapshotDateUtc).FirstOrDefault();
        }

        private static double MedianOf(IEnumerable<double> values)
        {
            var sorted = values.OrderBy(x => x).ToList();

            if (sorted.Count == 0)
                return 0;

            return sorted.Count % 2 != 0
                ? sorted[sorted.Count / 2]
                : (sorted[sorted.Count / 2] + sorted[(sorted.Count / 2) - 1]) / 2;
        }

        private static double Percentile(List<double> sorted, double fraction)
        {
            if (sorted.Count == 0)
                return 0;

            var index = (int)Math.Round(fraction * (sorted.Count - 1));

            return sorted[Math.Clamp(index, 0, sorted.Count - 1)];
        }

        private record Scored(
            double Actual, double Predicted, int NeighboursUsed, double NearestMeters, PropertyListing Listing)
        {
            /// <summary>Did the quoted low-to-high range actually contain the asking price?</summary>
            public bool InBand { get; init; }

            /// <summary>How wide that range was, as a percentage of the estimate.</summary>
            public double BandWidthPercent { get; init; }

            public double AbsolutePercentError => Actual <= 0 ? 0 : Math.Abs(Predicted - Actual) / Actual * 100;
        }
    }
}
