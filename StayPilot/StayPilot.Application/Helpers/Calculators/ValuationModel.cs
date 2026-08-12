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
        /// How many of the fitted listings actually carry this feature - the evidence behind
        /// this row, as opposed to the size of the fit. A sea view measured on 2,000 listings
        /// out of 14,000 is a very different claim from a garage measured on 9,000, and showing
        /// the training total against both made them look equally well-evidenced.
        ///
        /// For <see cref="PremiumFeatures.BeachProximity"/> there is no "has it" - this counts
        /// the listings with a usable beach distance, which is what the term was read from.
        /// </summary>
        public int ListingsWithFeature { get; set; }

        /// <summary>
        /// The best this feature is worth under the conditions that favour it most, when
        /// <see cref="Percent"/> is an average over conditions that differ enormously. Null for
        /// features whose worth does not depend on anything - most of them.
        ///
        /// Only the sea view has one today: the model carries a SeaView x distance term, so the
        /// headline ~9% is the average across every distance, including "sea view" adverts 5km
        /// inland where it means a sliver on the horizon. On the beachfront the same fit says
        /// far more. Both numbers come out of the same regression - this is not a nicer estimate,
        /// it is a different question, which is why <see cref="MaximumBasis"/> is mandatory
        /// alongside it.
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
        /// What <see cref="Percent"/> is measured against, when a bare "if present" would
        /// mislead. Null for ordinary features.
        ///
        /// This exists because two of the values are not flat premiums. Beach proximity is per
        /// halving of distance, and a sea view is worth far more on the beachfront than inland -
        /// its headline figure is the average across all distances, which on its own reads as
        /// "a sea view is worth less than a garage" when at 100m it is worth double.
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
    /// Prices a property from the listings we have collected.
    ///
    /// Two stages. First a hedonic regression on ln(price per m²): what the property IS - its
    /// size, rooms, condition, features, distance to the beach, plus a per-market-area level.
    /// Then a neighbourhood correction: the regression's leftover error is not random, it is
    /// geographic (one street is simply dearer than the next), so we shift the prediction by
    /// the median error of the nearest handful of known listings. That second stage is where
    /// most of the accuracy lives - MarketArea alone lumps whole towns together.
    ///
    /// Measured by 5-fold backtest over the collected listings: median absolute error ~13%,
    /// with ~41% of predictions inside 10%. For comparison, the same data priced by comp median
    /// alone (which is what this replaced) scores ~18.5%, and the old per-feature premium layer
    /// improved on that by 0.1 points.
    ///
    /// One honest limit: listings are ASKING prices. This predicts what a seller will ask, not
    /// what a buyer will pay - no amount of modelling can extract the latter from this data.
    /// </summary>
    public class ValuationModel
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
        /// Past this distance a "nearest listing" is not a neighbour and its error says nothing
        /// about this street. Without this bound a property with a broken coordinate (a missing
        /// minus sign on the longitude puts Portugal in the Mediterranean) silently borrows the
        /// correction from listings a thousand kilometres away.
        /// </summary>
        private const double MaximumNeighbourMeters = 25_000;

        /// <summary>
        /// Beach distances beyond this are treated as unknown rather than believed. Nowhere in
        /// Portugal is 50km from the sea, so a larger number is a broken coordinate, and feeding
        /// it to a log term produces an enormous bogus discount instead of an obvious error.
        /// </summary>
        private const int ImplausibleBeachMeters = 50_000;

        /// <summary>
        /// The distance the sea view's "up to" figure is quoted at. 100m is beachfront in
        /// practice and sits inside the collected data, so the number is read off the fitted
        /// curve rather than extrapolated past the end of it. Quoting it at 50m would give a
        /// bigger headline from thinner evidence, which is the trade this constant refuses.
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
        /// Typical size of the regression's own error, on the log scale - so multiplying a
        /// prediction by e^±this gives a roughly two-thirds-likely range. Deliberately measured
        /// from the fit rather than hardcoded, so it tracks the data instead of drifting away
        /// from it. It ignores the improvement the neighbourhood correction brings, which makes
        /// the range it produces a little wide - erring toward honest rather than flattering.
        /// </summary>
        public double PredictionSpread { get; }

        /// <summary>
        /// Does this property have that feature? Single source of truth for the mapping between
        /// a <see cref="PremiumFeatures"/> value and the property field behind it, so callers
        /// pricing a feature and the model measuring it can never disagree.
        /// Returns false for <see cref="PremiumFeatures.BeachProximity"/> and anything else that
        /// is not a yes/no feature.
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

            // Skip(1) drops one level from each dummy set - the baseline that the others are
            // measured against. Without it the set adds up to the intercept: every listing
            // scores 1 in exactly one column, so "all the dummies" and "the intercept" become
            // the same column and the fit has no unique answer (it can add any amount to one
            // and subtract it from the other). That produces enormous cancelling coefficients
            // rather than an obvious failure, so it has to be prevented, not detected.
            //
            // Areas need this as much as typology does: thin areas fall outside the dummy set
            // and would otherwise be the only thing keeping it from summing to one.
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

            // The measured-not-described features. Every one of these is read off a structured
            // field the source recorded, never off the advert's prose - wording tracks the price
            // bracket a property is marketed in, so a premium read from it would be measuring
            // the copywriting. Four of the five were already fitted columns and simply had
            // nowhere to be reported.
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
        /// Attaches the beachfront figure to the sea view row - what the same fit says a sea view
        /// is worth at <see cref="BeachfrontMeters"/>, which is where the feature is actually
        /// bought and sold. The market-wide average buries that under every "sea view" advert
        /// kilometres inland.
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
        /// What a sea view is worth for a property the given distance from the beach.
        ///
        /// The model carries a SeaView x ln(distance) term, so this is not one number: a view
        /// from the beachfront is worth several times one from 5km inland, where "sea view"
        /// usually means a sliver on the horizon. The headline percentage is the value at the
        /// typical distance, which is why it can look smaller than a garage - at 100m it is not.
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
        /// Turns a log-scale coefficient into a percentage change.
        ///
        /// Clamped before the exponential: a degenerate fit can throw out a coefficient of 700,
        /// and e^700 overflows a decimal - which used to surface as an OverflowException from
        /// deep inside the fit rather than as a bad number. ±5 spans ×0.007 to ×148, far outside
        /// anything a real feature could be worth, so clamping never touches a genuine result.
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
