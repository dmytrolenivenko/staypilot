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
        /// How many fitted listings carry this feature - the evidence behind this row, not the
        /// size of the fit. For BeachProximity: listings with a usable beach distance.
        /// </summary>
        public int ListingsWithFeature { get; set; }

        /// <summary>
        /// What the feature is worth under the conditions that favour it most, when
        /// <see cref="Percent"/> averages over conditions that differ a lot. Sea view only
        /// today; null for the rest. <see cref="MaximumBasis"/> is mandatory alongside it.
        /// </summary>
        public decimal? MaximumPercent { get; set; }

        /// <summary>
        /// The conditions <see cref="MaximumPercent"/> applies under, for example "within 100m of
        /// the beach". Never null when <see cref="MaximumPercent"/> is set - an "up to" figure
        /// with no stated conditions is a marketing claim, not a measurement.
        /// </summary>
        public string? MaximumBasis { get; set; }

        /// <summary>
        /// False when the confidence range straddles zero - meaning the data cannot tell us
        /// whether this feature is worth anything at all. Showing "-0.2%" for one of these
        /// reads as "it makes the flat cheaper", which is not what it means.
        /// </summary>
        public bool IsMeasurable => LowerPercent > 0 || UpperPercent < 0;

        /// <summary>
        /// What <see cref="Percent"/> is measured against, when "if present" would mislead -
        /// beach proximity is per halving of distance. Null for ordinary features.
        /// </summary>
        public string? Basis { get; set; }
    }

    /// <summary>
    /// One valuation, plus how much local evidence stood behind it.
    /// </summary>
    public class ValuationPrediction
    {
        public decimal PricePerM2 { get; set; }

        /// <summary>
        /// Distance to the nearest listing we learned from. Large means we are extrapolating
        /// into an area with no data, which the caller should reflect in its confidence.
        /// </summary>
        public double NearestComparableMeters { get; set; }

        /// <summary>How many nearby listings fed the neighbourhood correction (0 = none).</summary>
        public int LocalComparablesUsed { get; set; }
    }

    /// <summary>
    /// Prices a property from the collected listings, in two stages: a hedonic regression on
    /// ln(price per m²), then a neighbourhood correction from the nearest listings' errors -
    /// which is where most of the accuracy lives.
    ///
    /// 5-fold backtest: ~13% median error, vs ~18.5% for comp median alone.
    /// Caveat: these are ASKING prices, so it predicts what a seller asks, not what a buyer pays.
    ///
    /// Internal on purpose: <see cref="PremiumFeaturesCalculator"/> is the way in.
    /// </summary>
    internal class ValuationModel
    {
        /// <summary>A market area needs at least this many listings to get its own price level.</summary>
        private const int MinimumListingsPerArea = 15;

        /// <summary>
        /// How many nearby listings feed the neighbourhood correction. Backtested at 3/5/7/10/15:
        /// 7-10 was the flat optimum (13.0%), 3 was noticeably worse (13.5%). Ten is chosen from
        /// the middle of the flat stretch so the result does not hinge on the exact number.
        /// </summary>
        private const int NeighbourCount = 10;

        /// <summary>Below this many listings there is nothing worth fitting.</summary>
        private const int MinimumTrainingListings = 100;

        /// <summary>
        /// Past this, a "nearest listing" isn't a neighbour. Also stops a broken coordinate
        /// borrowing the correction from a thousand km away.
        /// </summary>
        private const double MaximumNeighbourMeters = 25_000;

        /// <summary>
        /// Beach distances beyond this are treated as unknown rather than believed. Nowhere in
        /// Portugal is 50km from the sea, so a larger number is a broken coordinate, and feeding
        /// it to a log term produces an enormous bogus discount instead of an obvious error.
        /// </summary>
        private const int ImplausibleBeachMeters = 50_000;

        /// <summary>
        /// Where the sea view's "up to" figure is quoted. 100m is beachfront and sits inside
        /// the data, so it's read off the curve rather than extrapolated past the end of it.
        /// </summary>
        private const int BeachfrontMeters = 100;

        /// <summary>
        /// The yes/no features we report on, in the order they occupy model columns.
        /// HasAirConditioning is absent on purpose: the data has thousands of trues and not one
        /// explicit false, so it encodes "the advert mentioned AC", not "the flat has AC".
        /// </summary>
        private static readonly (PremiumFeatures Feature, Func<ValuationSubject, bool> Read)[] BooleanFeatures =
        {
            (PremiumFeatures.HasElevator, x => x.HasElevator),
            (PremiumFeatures.HasTerrace, x => x.HasTerrace),
            (PremiumFeatures.HasGarage, x => x.HasGarage),
            (PremiumFeatures.HasSwimmingPool, x => x.HasSwimmingPool),
            (PremiumFeatures.IsFurnished, x => x.IsFurnished),
            (PremiumFeatures.HasParking, x => x.HasParking),
            (PremiumFeatures.HasSeaView, x => x.HasSeaView),
            (PremiumFeatures.HasCityView, x => x.HasCityView),
            (PremiumFeatures.IsNewBuild, x => x.Condition == PropertyCondition.NewBuild),
            (PremiumFeatures.IsRenovated, x => x.Condition == PropertyCondition.Renovated),
        };

        // Where each non-dummy column sits, named rather than counted out at each use site.
        // BuildRow lays them down in exactly this order; the two must move together, and
        // naming them is what makes that checkable instead of a game of off-by-one.
        private static int NeedsRenovationColumn => 1 + BooleanFeatures.Length;   // column 0 is the intercept
        private static int BathroomsColumn => NeedsRenovationColumn + 3;
        private static int BalconyColumn => NeedsRenovationColumn + 4;
        private static int BeachColumn => NeedsRenovationColumn + 5;
        private static int SeaViewBeachColumn => NeedsRenovationColumn + 6;
        private static int FloorColumn => NeedsRenovationColumn + 7;
        private static int EnergyGradeColumn => NeedsRenovationColumn + 11;

        // Fitted shape. Every one of these is needed to rebuild an identical row at predict
        // time - the column layout must match exactly what was fitted, or coefficients get
        // applied to the wrong thing.
        private readonly List<int> _areaColumns;
        private readonly List<Typology> _typologyColumns;
        private readonly List<PropertyType> _propertyTypeColumns;
        private readonly double _averageLogArea;
        private readonly double _averageLogBeachMeters;
        private readonly double _averageConstructionYear;
        private readonly double _averageEnergyGrade;
        private readonly double _averageBathrooms;
        private readonly double _averageBalconies;
        private readonly double _medianFloor;
        private readonly double _medianBeachMeters;
        private readonly double[] _coefficients;
        private readonly List<(double Latitude, double Longitude, double Residual)> _residuals;

        /// <summary>What each feature turned out to be worth, with confidence ranges.</summary>
        public IReadOnlyList<FeatureEffect> FeatureEffects { get; }

        /// <summary>How many listings the fit learned from.</summary>
        public int TrainingListings { get; }

        /// <summary>
        /// Share of the price variation the regression explains, before the neighbourhood
        /// correction. Useful as a sanity check, not as an accuracy measure.
        /// </summary>
        public double RSquared { get; }

        /// <summary>
        /// The regression's typical error on the log scale: e^±this is a ~two-thirds range.
        /// Measured from the fit, and ignores the neighbourhood correction - so it errs wide.
        /// </summary>
        public double PredictionSpread { get; }

        /// <summary>
        /// The typical property, in the same terms the per-unit features are measured in -
        /// what "three bathrooms" gets compared against. Floor and beach use medians because
        /// both are skewed.
        /// </summary>
        public double MarketAverageBathrooms => _averageBathrooms;

        /// <inheritdoc cref="MarketAverageBathrooms"/>
        public double MarketAverageBalconies => _averageBalconies;

        /// <inheritdoc cref="MarketAverageBathrooms"/>
        public double MarketAverageEnergyGrade => _averageEnergyGrade;

        /// <inheritdoc cref="MarketAverageBathrooms"/>
        public double MarketMedianFloor => _medianFloor;

        /// <inheritdoc cref="MarketAverageBathrooms"/>
        public double MarketMedianBeachMeters => _medianBeachMeters;

        /// <summary>
        /// Does this property have that feature? One source of truth for feature -> field, so
        /// pricing and measuring can't disagree. False for anything that isn't yes/no.
        /// </summary>
        public static bool HasFeature(ValuationSubject subject, PremiumFeatures feature)
        {
            // Needing renovation is a plain yes/no like the rest, but it lives in its own column
            // rather than in BooleanFeatures, so it has to be read separately. It is the only
            // one of the newer features that works this way - a grade step, a bathroom and a
            // floor are quantities, and "does this property have a floor" is not a question.
            if (feature == PremiumFeatures.NeedsRenovation)
                return subject.Condition == PropertyCondition.NeedsRenovation;

            var match = BooleanFeatures.FirstOrDefault(x => x.Feature == feature);

            return match.Read is not null && match.Read(subject);
        }

        /// <summary>
        /// Fits the model to the listings given. Each listing needs at least one price snapshot,
        /// a positive area and a sane price; anything else is dropped rather than fitted to.
        /// </summary>
        /// <exception cref="InvalidOperationException">Not enough usable listings to fit.</exception>
        public static ValuationModel Fit(IEnumerable<PropertyListing> listings)
        {
            var training = new List<(ValuationSubject Subject, double LogPricePerM2)>();

            foreach (var listing in listings)
            {
                var snapshot = listing.ListingSnapshots
                    .OrderByDescending(x => x.SnapshotDateUtc)
                    .FirstOrDefault();

                // Drop the unusable rather than let a zero area or a placeholder price bend
                // the fit. The bounds are deliberately wide - this removes broken data, not
                // expensive property.
                if (snapshot is null || listing.AreaM2 <= 0 || snapshot.PricePerM2 <= 0)
                    continue;

                if (snapshot.PricePerM2 < 100 || snapshot.PricePerM2 > 100_000)
                    continue;

                training.Add((ValuationSubject.FromListing(listing), Math.Log((double)snapshot.PricePerM2)));
            }

            if (training.Count < MinimumTrainingListings)
                throw new InvalidOperationException(
                    $"Need at least {MinimumTrainingListings} usable listings to fit a valuation model, found {training.Count}.");

            return new ValuationModel(training);
        }

        private ValuationModel(List<(ValuationSubject Subject, double LogPricePerM2)> training)
        {
            TrainingListings = training.Count;

            var subjects = training.Select(x => x.Subject).ToList();

            // Skip(1) drops one level per dummy set - the baseline the rest are measured
            // against. Keep them all and the set equals the intercept, so the fit has no
            // unique answer - and it fails silently, as huge cancelling coefficients.
            _areaColumns = subjects
                .GroupBy(x => x.MarketAreaId)
                .Where(g => g.Count() >= MinimumListingsPerArea)
                .Select(g => g.Key)
                .OrderBy(x => x)
                .Skip(1)
                .ToList();

            _typologyColumns = subjects.Select(x => x.Typology).Distinct().OrderBy(x => x).Skip(1).ToList();
            _propertyTypeColumns = subjects.Select(x => x.PropertyType).Distinct().OrderBy(x => x).Skip(1).ToList();

            _medianFloor = Median(subjects.Where(x => x.Floor.HasValue).Select(x => (double)x.Floor!.Value));
            _medianBeachMeters = Median(subjects.Where(x => x.DistanceToBeachMeters.HasValue)
                                               .Select(x => (double)x.DistanceToBeachMeters!.Value));

            if (_medianBeachMeters <= 0)
                _medianBeachMeters = 1000;

            // Centring: subtract the average before squaring or interacting. Without this the
            // HasSeaView coefficient would mean "a sea view one metre from the water" (because
            // that is where ln(distance) hits zero) instead of "a sea view at a typical
            // distance", and the number on screen would be nonsense.
            _averageLogArea = subjects.Average(x => Math.Log(Math.Max(1, x.AreaM2)));
            _averageLogBeachMeters = subjects.Average(x => Math.Log(BeachMetersOf(x)));
            _averageConstructionYear = subjects.Where(x => x.ConstructionYear.HasValue)
                .Select(x => (double)x.ConstructionYear!.Value)
                .DefaultIfEmpty(2000)
                .Average();

            // Same centring reason as the others: without it the intercept would mean "a flat
            // rated G", the worst grade on the scale, rather than a typical one.
            _averageEnergyGrade = subjects.Where(x => x.EnergyGradeScore.HasValue)
                .Select(x => (double)x.EnergyGradeScore!.Value)
                .DefaultIfEmpty(4)
                .Average();

            // Bathrooms and balconies go into the fit raw rather than centred, so the model
            // itself never needed their averages. A valuation does: what three bathrooms are
            // worth is what they are worth ABOVE what the market usually has, and crediting all
            // three against zero would pay every property a premium for being ordinary.
            _averageBathrooms = subjects.Average(x => (double)x.Bathrooms);
            _averageBalconies = subjects.Average(x => (double)x.BalconyCount);

            var rows = subjects.Select(BuildRow).ToList();
            var targets = training.Select(x => x.LogPricePerM2).ToList();

            var fit = LeastSquares.Fit(rows, targets);
            _coefficients = fit.Coefficients;

            FeatureEffects = BuildFeatureEffects(fit, subjects);

            // The residual surface: where the regression is wrong, and where geographically.
            _residuals = new List<(double, double, double)>();

            var averageTarget = targets.Average();
            var residualSumOfSquares = 0d;
            var totalSumOfSquares = 0d;

            for (var i = 0; i < rows.Count; i++)
            {
                var residual = targets[i] - LeastSquares.Predict(_coefficients, rows[i]);

                residualSumOfSquares += residual * residual;
                totalSumOfSquares += Math.Pow(targets[i] - averageTarget, 2);

                var subject = subjects[i];

                if (subject.Latitude.HasValue && subject.Longitude.HasValue)
                    _residuals.Add(((double)subject.Latitude.Value, (double)subject.Longitude.Value, residual));
            }

            RSquared = totalSumOfSquares <= 0 ? 0 : 1 - residualSumOfSquares / totalSumOfSquares;
            PredictionSpread = Math.Sqrt(fit.ResidualVariance);
        }

        /// <summary>
        /// Prices one property: the regression, then nudged by what nearby listings actually did.
        /// </summary>
        public ValuationPrediction PredictPricePerM2(ValuationSubject subject)
        {
            if (subject.AreaM2 <= 0)
                throw new ArgumentException("Cannot price a property with no floor area.", nameof(subject));

            var logPricePerM2 = LeastSquares.Predict(_coefficients, BuildRow(subject));

            var neighbours = NearestNeighbours(subject);

            if (neighbours.Count > 0)
                logPricePerM2 += Median(neighbours.Select(x => x.Residual));

            return new ValuationPrediction
            {
                PricePerM2 = (decimal)Math.Exp(logPricePerM2),
                LocalComparablesUsed = neighbours.Count,
                NearestComparableMeters = neighbours.Count == 0 ? double.MaxValue : neighbours.Min(x => x.Distance),
            };
        }

        /// <summary>
        /// The nearest known listings by real-world distance. Empty when the property has no
        /// coordinates, in which case the caller just gets the plain regression.
        /// </summary>
        private List<(double Distance, double Residual)> NearestNeighbours(ValuationSubject subject)
        {
            if (!subject.Latitude.HasValue || !subject.Longitude.HasValue)
                return new List<(double, double)>();

            var latitude = (double)subject.Latitude.Value;
            var longitude = (double)subject.Longitude.Value;

            return _residuals
                .Select(x => (
                    Distance: Calculator.CalculateDistanceMeters(latitude, longitude, x.Latitude, x.Longitude),
                    x.Residual))
                .Where(x => x.Distance <= MaximumNeighbourMeters)
                .OrderBy(x => x.Distance)
                .Take(NeighbourCount)
                .ToList();
        }

        /// <summary>
        /// Turns a property into the row of numbers the coefficients multiply. The order here
        /// IS the model's column order - it must stay identical between fitting and predicting.
        /// </summary>
        private double[] BuildRow(ValuationSubject subject)
        {
            var logArea = Math.Log(Math.Max(1, subject.AreaM2)) - _averageLogArea;
            var logBeach = Math.Log(BeachMetersOf(subject)) - _averageLogBeachMeters;

            var row = new List<double> { 1 };

            foreach (var (_, read) in BooleanFeatures)
            {
                row.Add(read(subject) ? 1 : 0);
            }

            // Needs-renovation is modelled but not reported as a "premium feature" - there is
            // no enum member for it, and only ~100 listings carry it.
            row.Add(subject.Condition == PropertyCondition.NeedsRenovation ? 1 : 0);

            row.Add(logArea);
            row.Add(logArea * logArea);                                 // price/m² falls with size, but not in a straight line
            row.Add(subject.Bathrooms);
            row.Add(subject.BalconyCount);
            row.Add(logBeach);
            row.Add(subject.HasSeaView ? logBeach : 0);                 // a "sea view" 5km inland is a weaker claim
            row.Add(subject.Floor ?? _medianFloor);
            row.Add(subject.Floor.HasValue ? 0 : 1);                    // flag the guess, so a gap is never read as floor 0
            row.Add((subject.ConstructionYear ?? _averageConstructionYear) - _averageConstructionYear);
            row.Add(subject.ConstructionYear.HasValue ? 0 : 1);

            // Energy rating, and a flag for the ~7% that do not state one. Without the flag a
            // missing certificate would be priced as an average rating, and the two are not the
            // same claim - properties that omit it are not typical, they are quieter about it.
            row.Add((subject.EnergyGradeScore ?? _averageEnergyGrade) - _averageEnergyGrade);
            row.Add(subject.EnergyGradeScore.HasValue ? 0 : 1);

            foreach (var typology in _typologyColumns)
            {
                row.Add(subject.Typology == typology ? 1 : 0);
            }

            foreach (var propertyType in _propertyTypeColumns)
            {
                row.Add(subject.PropertyType == propertyType ? 1 : 0);
            }

            foreach (var areaId in _areaColumns)
            {
                row.Add(subject.MarketAreaId == areaId ? 1 : 0);
            }

            return row.ToArray();
        }

        /// <summary>
        /// Reads the fitted coefficients back out as human-facing percentages. Boolean features
        /// convert straight across; beach proximity is rescaled to "per halving of distance",
        /// because a percentage for a continuous measurement otherwise means nothing.
        /// </summary>
        private List<FeatureEffect> BuildFeatureEffects(LeastSquaresFit fit, List<ValuationSubject> subjects)
        {
            var effects = new List<FeatureEffect>();

            for (var i = 0; i < BooleanFeatures.Length; i++)
            {
                var column = 1 + i;                                     // column 0 is the intercept
                var coefficient = fit.Coefficients[column];
                var margin = fit.ConfidenceMargin(column);
                var read = BooleanFeatures[i].Read;
                var feature = BooleanFeatures[i].Feature;

                var effect = new FeatureEffect
                {
                    Feature = feature,
                    Percent = ToPercent(coefficient),
                    LowerPercent = ToPercent(coefficient - margin),
                    UpperPercent = ToPercent(coefficient + margin),
                    ListingsWithFeature = subjects.Count(read),
                    // Deliberately null for every yes/no feature. The sea view's worth does vary
                    // with distance, but that is carried by MaximumPercent below rather than by
                    // redefining what the headline percentage means - the headline stays "the
                    // average across the market", the same as every other row.
                    Basis = null,
                };

                if (feature == PremiumFeatures.HasSeaView)
                    ApplyBeachfrontMaximum(effect);

                effects.Add(effect);
            }

            // The coefficient is per unit of ln(metres) and negative (further out is cheaper).
            // Halving the distance changes ln by -ln(2), so the gain is -coefficient * ln(2).
            var halving = Math.Log(2);
            var beachGain = -fit.Coefficients[BeachColumn] * halving;
            var beachMargin = fit.ConfidenceMargin(BeachColumn) * halving;

            effects.Add(new FeatureEffect
            {
                Feature = PremiumFeatures.BeachProximity,
                Percent = ToPercent(beachGain),
                LowerPercent = ToPercent(beachGain - beachMargin),
                UpperPercent = ToPercent(beachGain + beachMargin),
                // The listings that actually told us something about distance. The rest were
                // filled in with the median, so counting them would inflate the evidence.
                ListingsWithFeature = subjects.Count(HasRecordedBeachDistance),
                Basis = "per halving of the distance to the beach",
            });

            // All read off structured fields, never the advert's prose - wording tracks the
            // price bracket, so a premium read from it would measure the copywriting.
            effects.Add(ScalarEffect(fit, PremiumFeatures.EnergyGrade, EnergyGradeColumn,
                subjects.Count(x => x.EnergyGradeScore.HasValue),
                "per grade step up the scale (G to A+)"));

            effects.Add(ScalarEffect(fit, PremiumFeatures.ExtraBathroom, BathroomsColumn,
                subjects.Count(x => x.Bathrooms > 0), "per bathroom"));

            effects.Add(ScalarEffect(fit, PremiumFeatures.FloorLevel, FloorColumn,
                subjects.Count(x => x.Floor.HasValue), "per floor up"));

            effects.Add(ScalarEffect(fit, PremiumFeatures.HasBalcony, BalconyColumn,
                subjects.Count(x => x.BalconyCount > 0), "per balcony"));

            // The one ordinary yes/no of the five - it needs no basis note, and it has been a
            // fitted column all along (see BuildRow) without an enum member to report it under.
            effects.Add(ScalarEffect(fit, PremiumFeatures.NeedsRenovation, NeedsRenovationColumn,
                subjects.Count(x => x.Condition == PropertyCondition.NeedsRenovation), basis: null));

            return effects;
        }

        /// <summary>
        /// Reads one column back as a reportable effect. Used for the features whose premium is
        /// per unit of something (a grade step, a bathroom, a floor) rather than "if present",
        /// which is why <paramref name="basis"/> is a parameter and not an afterthought.
        /// </summary>
        private static FeatureEffect ScalarEffect(
            LeastSquaresFit fit, PremiumFeatures feature, int column, int listingsWithFeature, string? basis)
        {
            var coefficient = fit.Coefficients[column];
            var margin = fit.ConfidenceMargin(column);

            return new FeatureEffect
            {
                Feature = feature,
                Percent = ToPercent(coefficient),
                LowerPercent = ToPercent(coefficient - margin),
                UpperPercent = ToPercent(coefficient + margin),
                ListingsWithFeature = listingsWithFeature,
                Basis = basis,
            };
        }

        /// <summary>
        /// What the same fit says a sea view is worth at <see cref="BeachfrontMeters"/> - the
        /// market average buries that under every "sea view" advert kilometres inland.
        /// </summary>
        private void ApplyBeachfrontMaximum(FeatureEffect effect)
        {
            // Nothing to quote when the fit could not find a sea view premium at all. Hanging an
            // "up to 30%" off a row whose confidence range straddles zero dresses noise up as a
            // ceiling, and it is the one version of this that would be indefensible.
            if (!effect.IsMeasurable)
                return;

            var beachfront = SeaViewPercentAt(BeachfrontMeters);

            // If the curve does not actually climb toward the water then the average already is
            // the best case, and restating it under a bolder label would be saying more than the
            // data does.
            if (beachfront <= effect.Percent)
                return;

            effect.MaximumPercent = beachfront;
            effect.MaximumBasis = $"within {BeachfrontMeters}m of the beach";
        }

        /// <summary>
        /// What a sea view is worth at a given distance from the beach. The model carries a
        /// SeaView x ln(distance) term, so beachfront is worth several times inland - which is
        /// why the headline figure can look smaller than a garage.
        /// </summary>
        public decimal SeaViewPercentAt(int beachMeters)
        {
            var seaViewColumn = 1 + Array.FindIndex(BooleanFeatures, x => x.Feature == PremiumFeatures.HasSeaView);
            var interactionColumn = SeaViewBeachColumn;

            // Same sanity rule the prediction uses, so a broken coordinate cannot turn into a
            // wild sea-view figure here either.
            var metres = beachMeters <= 0 || beachMeters > ImplausibleBeachMeters
                ? Math.Max(50, _medianBeachMeters)
                : Math.Max(50, beachMeters);

            var centredLogDistance = Math.Log(metres) - _averageLogBeachMeters;

            return ToPercent(_coefficients[seaViewColumn] + _coefficients[interactionColumn] * centredLogDistance);
        }


        /// <summary>
        /// Distance to the beach, falling back to the typical listing when unknown and never
        /// below 50m (the log of a near-zero distance would swamp everything else).
        /// </summary>
        private double BeachMetersOf(ValuationSubject subject)
        {
            // Treat nonsense the same as missing: fall back to the typical listing rather than
            // believing a number that came from a broken coordinate.
            if (!HasRecordedBeachDistance(subject))
                return Math.Max(50, _medianBeachMeters);

            return Math.Max(50, subject.DistanceToBeachMeters!.Value);
        }

        /// <summary>
        /// Did this listing come with a believable distance to the beach? False for missing,
        /// zero-or-negative, and impossibly large values - all of which get the median instead.
        /// </summary>
        private static bool HasRecordedBeachDistance(ValuationSubject subject)
        {
            var recorded = subject.DistanceToBeachMeters;

            return recorded is > 0 and <= ImplausibleBeachMeters;
        }

        /// <summary>
        /// Log-scale coefficient to a percentage change. Clamped to ±5 first: a degenerate fit
        /// can produce e^700, which overflows decimal. ±5 is far outside any real premium.
        /// </summary>
        private static decimal ToPercent(double logCoefficient)
        {
            if (double.IsNaN(logCoefficient))
                return 0;

            var clamped = Math.Clamp(logCoefficient, -5, 5);

            return (decimal)((Math.Exp(clamped) - 1) * 100);
        }

        private static double Median(IEnumerable<double> values)
        {
            var sorted = values.OrderBy(x => x).ToList();

            if (sorted.Count == 0)
                return 0;

            return sorted.Count % 2 != 0
                ? sorted[sorted.Count / 2]
                : (sorted[sorted.Count / 2] + sorted[(sorted.Count / 2) - 1]) / 2;
        }
    }
}
