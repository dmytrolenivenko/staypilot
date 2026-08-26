using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Helpers.Calculators
{
    /// <summary>
    /// What one feature is worth, with an honest range around it.
    /// </summary>
    public class FeatureEffect
    {
        public PremiumFeatures Feature { get; set; }

        /// <summary>Best estimate, as a percentage (12.5 means +12.5%).</summary>
        public decimal Percent { get; set; }

        /// <summary>Bottom of the 95% confidence range, same units.</summary>
        public decimal LowerPercent { get; set; }

        /// <summary>Top of the 95% confidence range, same units.</summary>
        public decimal UpperPercent { get; set; }

        /// <summary>
        /// How many compared listings carry this feature - the evidence behind this row, not the
        /// size of the fit.
        /// </summary>
        public int ListingsWithFeature { get; set; }

        /// <summary>
        /// What the feature is worth under the conditions that favour it most, when
        /// <see cref="Percent"/> averages over conditions that differ a lot. The sea view and
        /// the lift today; null for the rest. <see cref="MaximumBasis"/> is mandatory alongside
        /// it - an "up to" with no stated conditions is a marketing claim, not a measurement.
        /// </summary>
        public decimal? MaximumPercent { get; set; }

        /// <inheritdoc cref="MaximumPercent"/>
        public string? MaximumBasis { get; set; }

        /// <summary>
        /// False when the confidence range straddles zero - meaning the data cannot tell us
        /// whether this feature is worth anything at all. Showing "-0.2%" for one of these
        /// reads as "it makes the flat cheaper", which is not what it means.
        /// </summary>
        public bool IsMeasurable => LowerPercent > 0 || UpperPercent < 0;

        /// <summary>
        /// What <see cref="Percent"/> is measured against, when "if present" would mislead -
        /// for example "per bathroom". Null for ordinary yes/no features.
        /// </summary>
        public string? Basis { get; set; }
    }

    /// <summary>
    /// What each premium feature is worth, by comparing listings that are alike in everything the
    /// data can see except that feature.
    ///
    /// Three steps, and there is nothing else to it:
    ///
    ///   1. Put every listing in a GROUP of comparable flats - same 500m patch of the same market
    ///      area, same typology, same 20m² size band. Groups with only one listing are dropped:
    ///      there is nothing to compare them against.
    ///   2. Subtract each group's own averages from its listings. Whatever is the same across a
    ///      whole group - the street, the town, the price level there - becomes zero and cannot
    ///      reach the answer. What is left is how the flats in one group differ from each other.
    ///   3. Fit all the features at once on what is left. Fitting them together is what stops a
    ///      garage being paid for the pool that tends to come with it.
    ///
    /// Step 3 is a regression rather than a plain average of "with it" minus "without it" for one
    /// good reason: features arrive together. Averaged one at a time on these listings, a lift
    /// reads as worth MORE on the ground floor than on the fourth - because flats with lifts are
    /// in newer buildings, and nothing was holding that still.
    ///
    /// The groups are why the numbers are what they are. Compared inside a 500m patch alone, an
    /// extra bathroom read 8.9%; made to share a typology and a size band too it reads about 5%,
    /// and the rest was the bathroom standing in for "a bigger, different sort of flat".
    ///
    /// Cost: about a third of the listings sit in a group of one and are not used.
    /// <see cref="FeatureEffect.ListingsWithFeature"/> reports that honestly per feature.
    /// </summary>
    public class FeaturePremiumCalculator
    {
        /// <summary>Below this there is nothing worth measuring at all.</summary>
        private const int MinimumTrainingListings = 100;

        /// <summary>Averaged against itself, a group of one is a row of zeros. It carries nothing.</summary>
        private const int MinimumListingsPerGroup = 2;

        /// <summary>
        /// How many listings must carry a feature, among those that had something to compare
        /// against, before it gets a confidence range. Under this the estimate still comes back
        /// but with no range, so <see cref="FeatureEffect.IsMeasurable"/> reads false and the
        /// screen says "no measurable effect" rather than quoting a number off a handful of flats.
        /// </summary>
        private const int MinimumCarriers = 100;

        /// <summary>How wide a size band counts as "the same size".</summary>
        private const int SizeBandM2 = 20;

        /// <summary>Roughly 500m of latitude, and of longitude, at Portuguese latitudes.</summary>
        private const decimal PatchLatitude = 0.0045m;
        private const decimal PatchLongitude = 0.0058m;

        /// <summary>How far the conditional figure must beat the average before it is worth quoting.</summary>
        private const decimal MinimumUplift = 1m;

        /// <summary>
        /// How many control columns follow the measured ones: the lift's second tier, size, and
        /// the three "not stated" flags.
        /// </summary>
        private const int ControlColumns = 5;

        private readonly List<(ValuationSubject Subject, double LogPricePerM2)> _training;
        private readonly double _typicalLogArea;

        /// <summary>
        /// The features being measured, one per column, in column order. Built here rather than
        /// declared static so each one can simply read a listing - the typical values they fall
        /// back on are already known by the time this runs.
        /// </summary>
        private readonly Measured[] _measured;

        /// <summary>
        /// The lift's other tier. Deliberately not in <see cref="_measured"/>: it is not a row of
        /// its own, it is the other half of the lift's row. It sits in the first control column.
        /// </summary>
        private readonly Measured _liftHighUp;

        private FeaturePremiumCalculator(List<(ValuationSubject Subject, double LogPricePerM2)> training)
        {
            _training = training;
            _typicalLogArea = training.Average(x => Math.Log(Math.Max(1, x.Subject.AreaM2)));

            var typicalFloor = Calculator.Median(training.Where(x => x.Subject.Floor.HasValue)
                                              .Select(x => (double)x.Subject.Floor!.Value));

            var typicalEnergyGrade = training.Where(x => x.Subject.EnergyGradeScore.HasValue)
                .Select(x => (double)x.Subject.EnergyGradeScore!.Value)
                .DefaultIfEmpty(4)
                .Average();

            _measured = BuildMeasuredFeatures(typicalFloor, typicalEnergyGrade);

            _liftHighUp = new Measured(
                PremiumFeatures.HasElevator,
                x => x.HasElevator && ValuationSubject.IsHighUp(x) ? 1 : 0,
                x => x.HasElevator && ValuationSubject.IsHighUp(x),
                $"on floor {ValuationSubject.LiftMattersFromFloor} or above");

            FeatureEffects = Measure(training);
        }

        /// <summary>What each feature turned out to be worth, with confidence ranges.</summary>
        public IReadOnlyList<FeatureEffect> FeatureEffects { get; }

        /// <summary>How many usable listings the measurements were drawn from.</summary>
        public int TrainingListings => _training.Count;

        /// <summary>
        /// Reads the listings and measures every feature. What counts as a real listing is
        /// <see cref="ListingQuality"/>'s call - a mis-parsed 2m² flat at €174,500/m² would drag
        /// every number with it.
        /// </summary>
        /// <exception cref="InvalidOperationException">Not enough usable listings.</exception>
        public static FeaturePremiumCalculator Fit(IEnumerable<PropertyListing> listings)
        {
            var calculator = TryFit(listings, out var usableListings);

            return calculator ?? throw new InvalidOperationException(
                $"Need at least {MinimumTrainingListings} usable listings to measure feature premiums, found {usableListings}.");
        }

        /// <summary>
        /// The same fit, but null instead of an exception when there is not enough to measure.
        /// Services use this one: too little data is an answer for the caller, not a crash.
        /// <paramref name="usableListings"/> is how many listings survived the quality filter,
        /// so the caller can put the real number in its error.
        /// </summary>
        public static FeaturePremiumCalculator? TryFit(IEnumerable<PropertyListing> listings, out int usableListings)
        {
            var training = ListingQuality.UsableSubjects(listings);

            usableListings = training.Count;

            return training.Count < MinimumTrainingListings ? null : new FeaturePremiumCalculator(training);
        }

        /// <summary>The fewest usable listings a fit needs.</summary>
        public static int MinimumListings => MinimumTrainingListings;

        /// <summary>
        /// Every feature the Feature Impact screen shows, in column order. Air conditioning is
        /// absent on purpose: the data holds thousands of trues and not one explicit false, so
        /// any number for it would measure whether the advert mentioned AC.
        /// </summary>
        private static Measured[] BuildMeasuredFeatures(double typicalFloor, double typicalEnergyGrade)
        {
            return new[]
            {
                // Below the third floor. The other tier is _liftHighUp, and MergeLiftTiers
                // decides which of the two becomes the row.
                new Measured(PremiumFeatures.HasElevator,
                    x => x.HasElevator && !ValuationSubject.IsHighUp(x) ? 1 : 0,
                    x => x.HasElevator && !ValuationSubject.IsHighUp(x),
                    $"below floor {ValuationSubject.LiftMattersFromFloor}"),

                new Measured(PremiumFeatures.HasTerrace, x => x.HasTerrace ? 1 : 0, x => x.HasTerrace),
                new Measured(PremiumFeatures.HasGarage, x => x.HasGarage ? 1 : 0, x => x.HasGarage),

                // Parking is read only where there is no garage. Four listings in five with a
                // garage also carry the parking flag, so measured as two free-standing features
                // the two fought over one signal and parking came back at 0.2% through zero.
                new Measured(PremiumFeatures.HasParking,
                    x => x.HasParking && !x.HasGarage ? 1 : 0,
                    x => x.HasParking && !x.HasGarage,
                    "parking without a garage"),

                new Measured(PremiumFeatures.HasSwimmingPool, x => x.HasSwimmingPool ? 1 : 0, x => x.HasSwimmingPool),
                new Measured(PremiumFeatures.IsFurnished, x => x.IsFurnished ? 1 : 0, x => x.IsFurnished),
                new Measured(PremiumFeatures.HasSeaView, x => x.HasSeaView ? 1 : 0, x => x.HasSeaView),
                new Measured(PremiumFeatures.HasCityView, x => x.HasCityView ? 1 : 0, x => x.HasCityView),

                new Measured(PremiumFeatures.IsNewBuild,
                    x => x.Condition == PropertyCondition.NewBuild ? 1 : 0,
                    x => x.Condition == PropertyCondition.NewBuild),

                new Measured(PremiumFeatures.IsRenovated,
                    x => x.Condition == PropertyCondition.Renovated ? 1 : 0,
                    x => x.Condition == PropertyCondition.Renovated),

                new Measured(PremiumFeatures.NeedsRenovation,
                    x => x.Condition == PropertyCondition.NeedsRenovation ? 1 : 0,
                    x => x.Condition == PropertyCondition.NeedsRenovation),

                // Yes/no, and read only where there is no terrace - both for the same reasons as
                // parking. Every listing ever collected has either no balcony or exactly one, so
                // "per balcony" priced a quantity that never varies; and over half the flats with
                // a terrace also flag a balcony, which let the terrace's worth leak in and come
                // back out as a balcony making a flat cheaper.
                new Measured(PremiumFeatures.HasBalcony,
                    x => x.BalconyCount > 0 && !x.HasTerrace ? 1 : 0,
                    x => x.BalconyCount > 0 && !x.HasTerrace,
                    "a balcony without a terrace"),

                // Walking distance to the sea, as a plain yes/no. This replaced a "per halving of
                // the distance" figure that was correct and unreadable: nobody could say what
                // "+4.6% per halving" meant for their own flat without doing logarithms. It is
                // only the reported premium that is a threshold - pricing itself works off comps.
                new Measured(PremiumFeatures.CloseToBeach,
                    x => ValuationSubject.IsCloseToBeach(x) ? 1 : 0,
                    ValuationSubject.IsCloseToBeach,
                    $"within {ValuationSubject.CloseToBeachMeters}m of the beach"),

                // Quantities, worth something per unit of themselves - which the basis note has
                // to say, or "per floor up" reads as "a flat with floors", which is every flat.
                // Where a listing did not say, the typical value stands in and the "not stated"
                // control column marks it, so the stand-in is never read as a real value.
                new Measured(PremiumFeatures.ExtraBathroom, x => x.Bathrooms, x => x.Bathrooms > 0, "per bathroom"),

                new Measured(PremiumFeatures.FloorLevel,
                    x => x.Floor ?? typicalFloor, x => x.Floor.HasValue, "per floor up"),

                new Measured(PremiumFeatures.EnergyGrade,
                    x => x.EnergyGradeScore ?? typicalEnergyGrade,
                    x => x.EnergyGradeScore.HasValue,
                    "per grade step up the scale (G to A+)"),
            };
        }

        /// <summary>The three steps, in order.</summary>
        /// <param name="withConditionals">
        /// False when this is the beachfront pass inside <see cref="ApplyBeachfrontSeaView"/> -
        /// otherwise it would measure its own beachfront subset forever.
        /// </param>
        private List<FeatureEffect> Measure(
            IReadOnlyList<(ValuationSubject Subject, double LogPricePerM2)> training, bool withConditionals = true)
        {
            var groups = training
                .GroupBy(x => GroupKeyFor(x.Subject))
                .Where(x => x.Count() >= MinimumListingsPerGroup)
                .Select(x => x.ToList())
                .ToList();

            var rows = new List<double[]>();
            var targets = new List<double>();

            foreach (var group in groups)
            {
                var built = group.Select(x => BuildRow(x.Subject)).ToList();
                var averagePrice = group.Average(x => x.LogPricePerM2);

                for (var column = 0; column < Columns; column++)
                {
                    var average = built.Average(x => x[column]);

                    foreach (var row in built)
                    {
                        row[column] -= average;
                    }
                }

                rows.AddRange(built);
                targets.AddRange(group.Select(x => x.LogPricePerM2 - averagePrice));
            }

            // No intercept: taking each group's averages off already removed one per group, which
            // is what makes this a within-group answer instead of a national average.
            var fit = FitOrNull(rows, targets);

            var effects = _measured
                .Select((measured, column) => BuildEffect(measured, column, fit, groups))
                .ToList();

            if (!withConditionals)
                return effects;

            MergeLiftTiers(effects, fit, groups);
            ApplyBeachfrontSeaView(effects);

            return effects;
        }

        /// <summary>
        /// The least-squares fit, or null when there is not enough left to fit. Every caller
        /// reads null as "we could not measure anything here".
        /// </summary>
        private LeastSquaresFit? FitOrNull(List<double[]> rows, List<double> targets)
        {
            if (rows.Count <= Columns)
                return null;

            // The rest of what LeastSquares.Fit refuses to fit, asked up front rather than
            // caught after the fact: one target per row, and more rows than the row is wide.
            if (rows.Count != targets.Count || rows.Count <= rows[0].Length)
                return null;

            return LeastSquares.Fit(rows, targets);
        }

        /// <summary>
        /// One fitted column as the rest of the app reads it. A column the data cannot speak to
        /// comes back with no range rather than a percentage dressed up as a finding.
        /// </summary>
        private static FeatureEffect BuildEffect(
            Measured measured,
            int column,
            LeastSquaresFit? fit,
            List<List<(ValuationSubject Subject, double LogPricePerM2)>> groups)
        {
            // Only groups where this feature actually differs are evidence about it. A flat with
            // a pool in a street where every flat has one proves nothing about pools.
            var carriers = groups
                .Where(group => group.Select(x => measured.Read(x.Subject)).Distinct().Count() > 1)
                .Sum(group => group.Count(x => measured.Carries(x.Subject)));

            var effect = new FeatureEffect
            {
                Feature = measured.Feature,
                Basis = measured.Basis,
                ListingsWithFeature = carriers,
            };

            if (fit is null || carriers == 0)
                return effect;

            effect.Percent = ToPercent(fit.Coefficients[column]);

            // Both bounds left at zero says "we could not measure this", which IsMeasurable reads
            // as a range failing to clear zero, without inventing a width.
            if (carriers < MinimumCarriers)
                return effect;

            var margin = fit.ConfidenceMargin(column);

            effect.LowerPercent = ToPercent(fit.Coefficients[column] - margin);
            effect.UpperPercent = ToPercent(fit.Coefficients[column] + margin);

            return effect;
        }

        /// <summary>
        /// The lift is measured as two columns - low down and high up - because that is the one
        /// place the data says the same feature is worth plainly different amounts. They become
        /// one row here.
        ///
        /// Which tier leads depends on what was measured. On these listings a lift below the
        /// third floor is not worth anything we can detect, so the high-floor figure becomes the
        /// whole row rather than an "up to" hung off a headline of zero.
        /// </summary>
        private void MergeLiftTiers(
            List<FeatureEffect> effects,
            LeastSquaresFit? fit,
            List<List<(ValuationSubject Subject, double LogPricePerM2)>> groups)
        {
            var lowDown = effects.First(x => x.Feature == PremiumFeatures.HasElevator);
            var highUp = BuildEffect(_liftHighUp, _measured.Length, fit, groups);

            if (!highUp.IsMeasurable)
                return;

            if (!lowDown.IsMeasurable)
            {
                effects[effects.IndexOf(lowDown)] = highUp;

                return;
            }

            if (highUp.Percent - lowDown.Percent < MinimumUplift)
                return;

            lowDown.MaximumPercent = highUp.Percent;
            lowDown.MaximumBasis = _liftHighUp.Basis;
        }

        /// <summary>
        /// What the same comparison says a sea view is worth close to the water. The market-wide
        /// figure averages beachfront views in with "sea view" adverts kilometres inland, so it
        /// is measured again on the beachfront listings alone.
        /// </summary>
        private void ApplyBeachfrontSeaView(List<FeatureEffect> effects)
        {
            var seaView = effects.First(x => x.Feature == PremiumFeatures.HasSeaView);

            if (!seaView.IsMeasurable)
                return;

            var beachfront = _training.Where(x => ValuationSubject.IsCloseToBeach(x.Subject)).ToList();

            if (beachfront.Count < MinimumTrainingListings)
                return;

            var measured = Measure(beachfront, withConditionals: false)
                .First(x => x.Feature == PremiumFeatures.HasSeaView);

            // Otherwise the average already is the best case, and restating it under a bolder
            // label would say more than the data does.
            if (!measured.IsMeasurable || measured.Percent - seaView.Percent < MinimumUplift)
                return;

            seaView.MaximumPercent = measured.Percent;
            seaView.MaximumBasis = $"within {ValuationSubject.CloseToBeachMeters}m of the beach";
        }

        /// <summary>
        /// The group a listing is compared inside. The 500m patch is always keyed inside the
        /// market area, never across it: a patch that straddled two would compare a seafront
        /// street against the town behind it and call the price gap a feature premium.
        /// </summary>
        private static string GroupKeyFor(ValuationSubject subject)
        {
            var patch = $"{Math.Floor(subject.Latitude / PatchLatitude)}:{Math.Floor(subject.Longitude / PatchLongitude)}";

            return $"{subject.MarketAreaId}|{patch}|{(int)subject.Typology}|{(int)subject.PropertyType}"
                 + $"|{subject.AreaM2 / SizeBandM2}";
        }

        /// <summary>How many numbers a row carries: the measured features, then the controls.</summary>
        private int Columns => _measured.Length + ControlColumns;

        /// <summary>
        /// A listing as a row of numbers. The first columns are the features being measured; the
        /// last are there only so those mean what they say. Size still matters inside a 20m² band,
        /// and the "not stated" flags stop a listing that never gave its floor being read as a
        /// ground-floor flat.
        /// </summary>
        private double[] BuildRow(ValuationSubject subject)
        {
            var row = new double[Columns];

            for (var i = 0; i < _measured.Length; i++)
            {
                row[i] = _measured[i].Read(subject);
            }

            row[_measured.Length] = _liftHighUp.Read(subject);
            row[_measured.Length + 1] = Math.Log(Math.Max(1, subject.AreaM2)) - _typicalLogArea;
            row[_measured.Length + 2] = subject.Floor.HasValue ? 0 : 1;
            row[_measured.Length + 3] = subject.EnergyGradeScore.HasValue ? 0 : 1;
            row[_measured.Length + 4] = ValuationSubject.KnowsBeachDistance(subject) ? 0 : 1;

            return row;
        }

        /// <summary>
        /// A log-scale coefficient as a percentage. Clamped to ±5 first: a degenerate fit can
        /// produce e^700, which overflows decimal, and ±5 is far outside any real premium.
        /// </summary>
        private static decimal ToPercent(double coefficient)
        {
            if (double.IsNaN(coefficient) || double.IsInfinity(coefficient))
                return 0;

            return (decimal)((Math.Exp(Math.Clamp(coefficient, -5, 5)) - 1) * 100);
        }

        /// <summary>
        /// One feature: how to read it off a listing, how to tell whether a listing carries it,
        /// and what its percentage is measured against. Its column is its position in
        /// <see cref="_measured"/> - counted by the array rather than written down by hand,
        /// which is one whole class of off-by-one gone.
        /// </summary>
        private sealed record Measured(
            PremiumFeatures Feature,
            Func<ValuationSubject, double> Read,
            Func<ValuationSubject, bool> Carries,
            string? Basis = null);
    }
}
