using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Contracts.Response.SubResponse;
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;
using System.Text.RegularExpressions;

namespace StayPilot.Application.Helpers.Calculators
{
    /// <summary>
    /// Everything one valuation produces: the price, the range around it, how much to trust it,
    /// what the property has earned, and which of its features the money is sitting in.
    /// </summary>
    public class PropertyEstimate
    {
        /// <summary>The headline number.</summary>
        public decimal MidPrice { get; set; }

        /// <summary>Low and high ends of the estimate. Equal to <see cref="MidPrice"/> only on a degenerate fit.</summary>
        public decimal MinPrice { get; set; }

        /// <inheritdoc cref="MinPrice"/>
        public decimal MaxPrice { get; set; }

        /// <summary>How much to trust it, judged on evidence near THIS property.</summary>
        public ValuationConfidence Confidence { get; set; }

        /// <summary>What the property has made since purchase. All zeros without a purchase price.</summary>
        public EquitySummary Equity { get; set; } = new();

        /// <summary>One line per premium feature the property actually has.</summary>
        public List<ValuationAdjustment> Adjustments { get; set; } = new();

        /// <summary>
        /// Which market area the price came from, and whether the coordinates chose it rather than
        /// the stored address. The comps are pulled from the same area, so the two halves of the
        /// answer cannot disagree about where the property is.
        /// </summary>
        public int LocatedMarketAreaId { get; set; }

        /// <inheritdoc cref="LocatedMarketAreaId"/>
        public bool LocatedByCoordinates { get; set; }
    }

    /// <summary>
    /// Values a property. The only way in: fit once over the collected listings, then estimate
    /// as many properties as you like against that fit.
    ///
    /// <code>
    /// var valuation = PropertyValuation.Fit(allListings);      // expensive, do it once
    /// var estimate  = valuation.Estimate(property, premiums);  // cheap, per property
    /// </code>
    ///
    /// What each feature is worth market-wide is a different question, answered by
    /// <see cref="FeaturePremiumCalculator"/>. This class never measures premiums - it is handed
    /// the already-measured ones, so a bathroom cannot read as +4% on the Feature Impact screen
    /// and +13% here.
    /// </summary>
    public class PropertyValuation
    {
        /// <summary>Below this many units above typical, a counted feature gets no row.</summary>
        private const decimal MinimumUnitsWorthReporting = 0.05m;

        private const double MediumConfidenceMeters = 5000;
        private const double HighConfidenceMeters = 1000;
        private const int HighConfidenceComparables = 10;

        /// <summary>Leap years included, so "years held" doesn't drift.</summary>
        private const decimal DaysPerYear = 365.25m;

        private readonly ValuationModel _model;

        private PropertyValuation(ValuationModel model)
        {
            _model = model;
        }

        /// <exception cref="InvalidOperationException">Not enough usable listings to fit.</exception>
        public static PropertyValuation Fit(IEnumerable<PropertyListing> listings)
        {
            return new PropertyValuation(ValuationModel.Fit(listings));
        }

        /// <summary>
        /// The same fit, but null instead of an exception when there are not enough usable
        /// listings. Services use this one: too little data is an answer for the caller, not a
        /// crash. <paramref name="usableListings"/> is how many we found, for the error message.
        /// </summary>
        public static PropertyValuation? TryFit(IEnumerable<PropertyListing> listings, out int usableListings)
        {
            var model = ValuationModel.TryFit(listings, out usableListings);

            return model is null ? null : new PropertyValuation(model);
        }

        /// <summary>The fewest usable listings a fit needs.</summary>
        public static int MinimumListings => ValuationModel.MinimumListings;

        /// <summary>How many listings the fit learned from.</summary>
        public int TrainingListings => _model.TrainingListings;

        /// <summary>
        /// How much a listing this far from a property is worth as evidence about it: full weight
        /// next door, half a kilometre out, fading beyond. Public because the comparables shown
        /// beside an estimate have to be summarised on the same scale the estimate itself uses -
        /// a plain average over a 2km circle in a beach town mixes two markets and reads as the
        /// dearer one. Measured around one Quarteira flat: the 17 comparables within 250m implied
        /// EUR 321,000 and the 300 beyond it implied EUR 460,000-473,000.
        /// </summary>
        public static double EvidenceWeightAtMeters(double metres)
        {
            return ValuationModel.NeighbourWeight(metres);
        }

        /// <summary>
        /// Prices one property and explains the answer.
        /// </summary>
        /// <param name="featureEffects">
        /// The premiums already measured - the same figures the Feature Impact screen shows.
        /// Passed in rather than measured here on purpose; see the class summary.
        /// </param>
        public PropertyEstimate Estimate(
            OwnedPropertyResponse property, IReadOnlyList<FeatureEffect> featureEffects)
        {
            var subject = ValuationSubject.FromOwnedProperty(property);
            var prediction = _model.PredictPricePerM2(subject);

            var midPrice = prediction.PricePerM2 * property.AreaM2;
            var (minPrice, maxPrice) = PriceRange(midPrice, prediction.Spread);

            return new PropertyEstimate
            {
                MidPrice = midPrice,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                Confidence = DetermineConfidence(prediction),
                Equity = BuildEquity(property.PurchasePrice, property.PurchaseDate, midPrice),
                Adjustments = BuildAdjustments(subject, midPrice, featureEffects),
                LocatedMarketAreaId = prediction.LocatedMarketAreaId,
                LocatedByCoordinates = prediction.LocatedByCoordinates,
            };
        }

        /// <summary>
        /// Which market area a property at these coordinates is priced as. Callers that also need
        /// comparables should pull them from this area rather than the stored one, so the model's
        /// answer and the comps beside it are talking about the same place.
        /// </summary>
        public int LocateMarketArea(decimal? latitude, decimal? longitude, int storedMarketAreaId)
        {
            return _model.LocateMarketArea(latitude, longitude, storedMarketAreaId);
        }

        /// <summary>
        /// Low and high ends of an estimate, from the model's own error rather than the comp
        /// spread - tightly clustered comps don't make a valuation more certain. The spread is
        /// per-property, so a flat with a street full of comps gets a tighter range than one the
        /// model is guessing at. Measured against held-out listings, the band contains the true
        /// asking price about three times in four.
        ///
        /// Internal rather than private only so the backtest can score the quoted range against
        /// held-out listings - that number is the whole point of showing a range at all.
        /// </summary>
        internal static (decimal MinPrice, decimal MaxPrice) PriceRange(decimal midPrice, double logSpread)
        {
            // Math.Exp is never negative and only returns 1 at a spread of zero, so a degenerate
            // fit collapses the range onto the estimate rather than inverting it.
            var spread = (decimal)Math.Exp(logSpread);

            return spread <= 1 ? (midPrice, midPrice) : (midPrice / spread, midPrice * spread);
        }

        /// <summary>
        /// How much to trust the estimate - judged on evidence near THIS property, not on the
        /// comp count. Listings only cover parts of the country, and a property outside them
        /// must not come back looking confident.
        /// </summary>
        private static ValuationConfidence DetermineConfidence(ValuationPrediction prediction)
        {
            if (prediction.LocalComparablesUsed >= HighConfidenceComparables
                && prediction.NearestComparableMeters <= HighConfidenceMeters)
                return ValuationConfidence.High;

            if (prediction.LocalComparablesUsed > 0
                && prediction.NearestComparableMeters <= MediumConfidenceMeters)
                return ValuationConfidence.Medium;

            return ValuationConfidence.Low;
        }

        /// <summary>
        /// What the property has made since purchase. All zeros without a purchase price - a
        /// gain measured against nothing still renders convincingly on screen.
        /// </summary>
        private static EquitySummary BuildEquity(
            decimal? purchasePrice, DateTime? purchaseDate, decimal currentEstimate)
        {
            var paid = purchasePrice ?? 0;
            var gainAmount = currentEstimate - paid;
            var gainPercent = paid > 0 ? gainAmount / paid * 100 : 0;

            // Fractional years (2.5, not 2) so the ROI maths is accurate.
            // A property saved without a purchase date carries the DateTime default, which reads as
            // two thousand years held. Anything before 1900 is an unset field, not a purchase.
            var yearsHeldExact = purchaseDate.HasValue && purchaseDate.Value.Year > 1900
                ? (decimal)(DateTime.UtcNow - purchaseDate.Value).TotalDays / DaysPerYear
                : 0m;

            var monthsHeldExact = yearsHeldExact * 12;

            return new EquitySummary
            {
                PurchasePrice = paid,
                CurrentEstimate = currentEstimate,
                GainAmount = gainAmount,
                GainPercent = Math.Round(gainPercent, 2),
                YearsHeld = (int)yearsHeldExact,

                // Under a year, annualising just magnifies noise -> stay at 0.
                RoiPerYear = paid > 0 && yearsHeldExact >= 1
                    ? Math.Round(gainPercent / yearsHeldExact, 2)
                    : 0,

                // Per month divides sensibly at any age, so recent buys still show something.
                RoiPerMonth = paid > 0 && monthsHeldExact > 0
                    ? Math.Round(gainPercent / monthsHeldExact, 2)
                    : 0,
            };
        }

        /// <summary>
        /// How much of <paramref name="estimatedPrice"/> each feature accounts for.
        ///
        /// Only what this property HAS earns a row. Yes/no features credit in full; counted ones
        /// only for what's ABOVE typical, else every property gets paid for being ordinary.
        /// Nothing is ever reported as a discount - a flat missing a feature is priced as not
        /// having it, which the estimate already reflects, not billed for it a second time here.
        ///
        /// Amounts compound, so they don't sum.
        /// </summary>
        private List<ValuationAdjustment> BuildAdjustments(
            ValuationSubject subject, decimal estimatedPrice, IReadOnlyList<FeatureEffect> featureEffects)
        {
            var adjustments = new List<ValuationAdjustment>();

            foreach (var effect in featureEffects)
            {
                var adjustment = CountedUnitsFor(subject, effect.Feature) is { } counted
                    ? CountedAdjustment(effect, counted, estimatedPrice)
                    : YesNoAdjustment(subject, effect, estimatedPrice);

                if (adjustment is not null)
                    adjustments.Add(adjustment);
            }

            return adjustments;
        }

        /// <summary>A "has it" row, or null when this property doesn't.</summary>
        private static ValuationAdjustment? YesNoAdjustment(
            ValuationSubject subject, FeatureEffect effect, decimal estimatedPrice)
        {
            if (!Carries(subject, effect.Feature))
                return null;

            var meetsCondition = effect.MaximumPercent is not null && MeetsConditionFor(subject, effect.Feature);

            return new ValuationAdjustment
            {
                Label = FriendlyFeatureName(effect.Feature),

                // A lift on the fourth floor and a sea view from the sand are worth measurably
                // more than the market average of one, so those two are credited at their
                // conditional figure rather than the headline.
                Amount = AmountFor(estimatedPrice, meetsCondition ? effect.MaximumPercent!.Value : effect.Percent, units: 1),

                // A premium that changes sign depending on how the comparison is drawn is not a
                // finding, however tight its confidence range looks - the breakdown greys it out
                // for the same reason it greys out a range that straddles zero.
                IsMeasurable = effect.IsMeasurable,

                // Only said when the conditional figure was used: "if present" needs no
                // explanation, and every row carrying an unasked-for note is how a breakdown
                // turns into a wall of text.
                Detail = meetsCondition ? effect.MaximumBasis : null,
            };
        }

        /// <summary>
        /// Does this property earn this row? Mostly a plain field read, with the pairs whose
        /// premium is measured against a narrower group than "has it": parking is priced where
        /// there is no garage, and a balcony where there is no terrace. Crediting both of a pair
        /// would pay the same outdoor space or the same car twice.
        ///
        /// The beach is deliberately absent - it is not a yes/no here, it fades with distance.
        /// See <see cref="BeachCredit"/>.
        /// </summary>
        private static bool Carries(ValuationSubject subject, PremiumFeatures feature)
        {
            return feature switch
            {
                PremiumFeatures.HasParking => subject.HasParking && !subject.HasGarage,
                PremiumFeatures.HasBalcony => subject.BalconyCount > 0 && !subject.HasTerrace,
                _ => ValuationModel.HasFeature(subject, feature),
            };
        }

        /// <summary>The conditions behind <see cref="FeatureEffect.MaximumBasis"/>, in code.</summary>
        private static bool MeetsConditionFor(ValuationSubject subject, PremiumFeatures feature)
        {
            return feature switch
            {
                PremiumFeatures.HasElevator => ValuationSubject.IsHighUp(subject),
                PremiumFeatures.HasSeaView => ValuationSubject.IsCloseToBeach(subject),
                _ => false,
            };
        }

        /// <summary>
        /// A counted row, or null when the property has no more of it than the market's typical.
        /// Below typical is not something this property has, so it gets no row: one bathroom in a
        /// market that averages two was being docked the price of a bathroom it never claimed.
        /// </summary>
        private static ValuationAdjustment? CountedAdjustment(
            FeatureEffect effect, CountedFeature counted, decimal estimatedPrice)
        {
            var unitsAboveTypical = counted.Units - counted.TypicalUnits;

            if (unitsAboveTypical < MinimumUnitsWorthReporting)
                return null;

            return new ValuationAdjustment
            {
                Label = FriendlyFeatureName(effect.Feature),
                Amount = AmountFor(estimatedPrice, effect.Percent, unitsAboveTypical),
                IsMeasurable = effect.IsMeasurable,
                Detail = counted.Detail,
            };
        }

        /// <summary>
        /// The share of the price owed to a feature: what disappears when its multiplier is
        /// divided back out. Floored at zero - a measured premium that came back negative means
        /// the feature is worth nothing here, not that the owner should be billed for having it.
        /// </summary>
        private static decimal AmountFor(decimal estimatedPrice, decimal percent, decimal units)
        {
            var multiplier = Math.Pow(1 + (double)percent / 100, (double)units);

            // A degenerate coefficient can zero the multiplier; dividing by it would throw.
            if (multiplier <= 0 || double.IsNaN(multiplier) || double.IsInfinity(multiplier))
                return 0;

            return Math.Max(0, Math.Round(estimatedPrice * (1 - 1 / (decimal)multiplier), 0));
        }

        /// <summary>
        /// How much of a counted feature this property has vs the market. Null for yes/no
        /// features - that's what tells <see cref="BuildAdjustments"/> which row to build.
        /// </summary>
        private CountedFeature? CountedUnitsFor(ValuationSubject subject, PremiumFeatures feature)
        {
            switch (feature)
            {
                case PremiumFeatures.CloseToBeach:
                    return BeachCredit(subject);

                case PremiumFeatures.ExtraBathroom:
                    return CountedFeature.Of(subject.Bathrooms, _model.MarketAverageBathrooms);

                // No stated floor, no claim - crediting one nobody recorded invents evidence.
                case PremiumFeatures.FloorLevel:
                    return subject.Floor is null
                        ? null
                        : CountedFeature.Of(subject.Floor.Value, _model.MarketMedianFloor);

                case PremiumFeatures.EnergyGrade:
                    return subject.EnergyGradeScore is null
                        ? null
                        : CountedFeature.Of(
                            subject.EnergyGradeScore.Value,
                            _model.MarketAverageEnergyGrade,
                            $"{ValuationSubject.GradeLetter(subject.EnergyGradeScore.Value)} vs " +
                            $"{ValuationSubject.GradeLetter((int)Math.Round(_model.MarketAverageEnergyGrade))} typical");

                default:
                    return null;
            }
        }

        /// <summary>
        /// How much of the beach premium this property earns, from full credit down to none:
        ///
        ///   within 500m   the whole premium - this is the distance it was measured on
        ///   500m - 2km    a shrinking share of it, straight-line
        ///   2km or more   nothing
        ///
        /// The fade is the point. Crediting 500m in full and 501m at zero is a cliff nobody
        /// would defend, and one metre is not the difference between a beach flat and an inland
        /// one. Between the two distances we know something about, a straight line is the least
        /// we can assume - it is an interpolation, and the only honest thing to call it.
        ///
        /// Null when the distance was never recorded, which earns no row at all: "we did not
        /// measure it" is not evidence of being anywhere.
        /// </summary>
        private static CountedFeature? BeachCredit(ValuationSubject subject)
        {
            if (!ValuationSubject.KnowsBeachDistance(subject))
                return null;

            var metres = subject.DistanceToBeachMeters!.Value;

            var share = Share(metres);

            return new CountedFeature(share, TypicalUnits: 0, DetailFor(metres, share));
        }

        /// <summary>The straight line from full credit at 500m to none at 2km.</summary>
        private static decimal Share(int metres)
        {
            if (metres <= ValuationSubject.CloseToBeachMeters)
                return 1m;

            if (metres >= ValuationSubject.BeachCreditEndsAtMeters)
                return 0m;

            var fadeOver = (decimal)(ValuationSubject.BeachCreditEndsAtMeters - ValuationSubject.CloseToBeachMeters);

            return (ValuationSubject.BeachCreditEndsAtMeters - metres) / fadeOver;
        }

        /// <summary>
        /// Says the distance, and says so when the row was only part-credited - a reader who sees
        /// a smaller number than the headline premium is owed the reason.
        /// </summary>
        private static string DetailFor(int metres, decimal share)
        {
            return share >= 1m
                ? $"{metres:N0}m from the beach"
                : $"{metres:N0}m from the beach - {share:P0} of the premium";
        }

        /// <summary>HasSeaView -> "Sea View", IsNewBuild -> "New Build", CloseToBeach -> "Close To Beach".</summary>
        public static string FriendlyFeatureName(PremiumFeatures feature)
        {
            var name = feature.ToString();

            if (name.StartsWith("Has"))
                name = name.Substring(3);
            else if (name.StartsWith("Is"))
                name = name.Substring(2);

            return Regex.Replace(name, "(?<=[a-z])([A-Z])", " $1");
        }

        /// <summary>
        /// One counted feature against the market's typical. A type, not a tuple, so the two
        /// numbers can't be swapped at a call site.
        /// </summary>
        private record CountedFeature(decimal Units, decimal TypicalUnits, string Detail)
        {
            /// <summary>
            /// Reads as "3 vs 1.8 typical". Pass <paramref name="detail"/> when the units
            /// aren't plain numbers - an energy grade shows as a letter.
            /// </summary>
            public static CountedFeature Of(int units, double typicalUnits, string? detail = null)
            {
                return new CountedFeature(
                    units,
                    (decimal)typicalUnits,
                    detail ?? $"{units} vs {typicalUnits:0.#} typical");
            }
        }
    }

    /// <summary>
    /// One price prediction, plus how much local evidence stood behind it.
    /// </summary>
    internal class ValuationPrediction
    {
        public decimal PricePerM2 { get; set; }

        /// <summary>
        /// Distance to the nearest listing we learned from. Large means we are extrapolating
        /// into an area with no data, which the caller should reflect in its confidence.
        /// </summary>
        public double NearestComparableMeters { get; set; }

        /// <summary>How many nearby listings fed the neighbourhood correction (0 = none).</summary>
        public int LocalComparablesUsed { get; set; }

        /// <summary>
        /// How uncertain THIS estimate is, on the log scale - the model's typical error, widened
        /// when there is little evidence nearby. A property surrounded by comps and one stranded
        /// in an area we know nothing about used to be handed the same range, which made the
        /// second one look far more certain than it was.
        /// </summary>
        public double Spread { get; set; }

        /// <summary>
        /// Which market area the price was actually taken from - the one the coordinates point at,
        /// which is not always the one the address said. See
        /// <see cref="ValuationModel.LocateMarketArea"/>.
        /// </summary>
        public int LocatedMarketAreaId { get; set; }

        /// <summary>
        /// True when the coordinates overrode the stored area. Worth showing: it is the difference
        /// between an estimate we placed ourselves and one that trusted a dropdown.
        /// </summary>
        public bool LocatedByCoordinates { get; set; }
    }

    /// <summary>
    /// Prices a property from the collected listings, in two stages: a hedonic regression on
    /// ln(price per m²), then a neighbourhood correction from the nearest listings' errors -
    /// which is where most of the accuracy lives.
    ///
    /// 5-fold backtest on 20,499 real listings: 10.4% median error, vs 14.0% for comp median
    /// alone and 29.0% for a flat market median.
    /// Caveat: these are ASKING prices, so it predicts what a seller asks, not what a buyer pays.
    ///
    /// It prices, and only prices. What features are worth is measured separately by
    /// <see cref="FeaturePremiumCalculator"/> - this class used to answer both questions from one
    /// set of coefficients, and the reporting half of it went unused for so long that nobody
    /// noticed the two screens could disagree.
    ///
    /// Internal on purpose: <see cref="PropertyValuation"/> is the way in.
    /// </summary>
    internal class ValuationModel
    {
        /// <summary>
        /// How many listings a place needs before it gets its own price level. Applied at each
        /// step of the market area -> municipality -> district fallback.
        /// </summary>
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
        /// How far from the fit a listing may sit before the second pass stops believing it,
        /// measured in robust standard deviations. Three keeps everything a real market throws
        /// up - on a normal spread it drops about one row in three hundred.
        /// </summary>
        private const double OutlierSigmas = 3.0;

        /// <summary>
        /// Past this, a "nearest listing" isn't a neighbour. Also stops a broken coordinate
        /// borrowing the correction from a thousand km away.
        /// </summary>
        private const double MaximumNeighbourMeters = 25_000;

        /// <summary>
        /// The distance at which a neighbour counts half as much as one next door. Roughly the
        /// scale over which a Portuguese town changes character - one side of the marina and the
        /// other are a kilometre and a different market apart.
        /// </summary>
        private const double NeighbourKernelMeters = 1_000;

        /// <summary>
        /// How many nearby listings vote on which market area a property is actually in. More
        /// than the neighbourhood correction uses, because this is a question about which side of
        /// a zone border the property sits on, and a single mis-filed neighbour must not decide it.
        /// </summary>
        private const int LocationVoteCount = 25;

        /// <summary>
        /// How close a listing has to be to get a vote on the market area. Past this the
        /// coordinates are no longer evidence of being in anyone's zone, so the stored area -
        /// whatever the address said - is the better answer.
        /// </summary>
        private const double LocationVoteMeters = 2_000;

        /// <summary>
        /// How much wider the range gets for a property with no local evidence at all: at most
        /// double the model's typical error, easing back to roughly its normal width once there
        /// are neighbours worth listening to.
        /// </summary>
        private const double ThinEvidenceWidening = 1.0;

        /// <summary>
        /// The yes/no features that occupy model columns, in that order.
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

        // Fitted shape. Every one of these is needed to rebuild an identical row at predict
        // time - the column layout must match exactly what was fitted, or coefficients get
        // applied to the wrong thing.
        private readonly List<string> _locationColumns;
        private readonly HashSet<int> _denseAreas;
        private readonly HashSet<int> _catchAllAreas;
        private readonly HashSet<string> _denseMunicipalities;
        private readonly HashSet<string> _denseDistricts;
        private readonly Dictionary<int, (string District, string Municipality)> _geographyByArea;
        private readonly List<Typology> _typologyColumns;
        private readonly List<PropertyType> _propertyTypeColumns;
        private readonly double _averageLogArea;
        private readonly double _averageLogBeachMeters;
        private readonly double _averageConstructionYear;
        private readonly double _averageEnergyGrade;
        private readonly double _averageBathrooms;
        private readonly double _medianFloor;
        private readonly double _medianBeachMeters;
        private readonly double[] _coefficients;
        private readonly List<(double Latitude, double Longitude, double Residual)> _residuals;

        /// <summary>
        /// Where the listings the fit learned from are, and which market area each was filed
        /// under. Separate from <see cref="_residuals"/> because this answers a different
        /// question - not "how wrong is the regression here" but "which zone is here".
        /// </summary>
        private readonly List<(double Latitude, double Longitude, int MarketAreaId)> _listingLocations;

        /// <summary>How many listings the fit learned from, after the outlier pass.</summary>
        public int TrainingListings { get; }

        /// <summary>
        /// How many admitted rows were re-advertisements of a property already counted. See
        /// <see cref="ListingQuality.DistinctProperties"/> for why they cannot be left in.
        /// </summary>
        public int DuplicateListings { get; }

        /// <summary>
        /// How many admitted listings the second pass then threw out. Worth watching: a jump
        /// here means the scraper started bringing back a new shape of broken row.
        /// </summary>
        public int DiscardedListings { get; }

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
        /// what "three bathrooms" gets compared against. Floor uses the median because it is
        /// skewed.
        /// </summary>
        public double MarketAverageBathrooms => _averageBathrooms;

        /// <inheritdoc cref="MarketAverageBathrooms"/>
        public double MarketAverageEnergyGrade => _averageEnergyGrade;

        /// <inheritdoc cref="MarketAverageBathrooms"/>
        public double MarketMedianFloor => _medianFloor;

        /// <summary>
        /// Does this property have that feature? One source of truth for feature -> field, so
        /// pricing and crediting can't disagree. False for anything that isn't yes/no.
        /// </summary>
        public static bool HasFeature(ValuationSubject subject, PremiumFeatures feature)
        {
            // Needing renovation is a plain yes/no like the rest, but it lives in its own column
            // rather than in BooleanFeatures, so it has to be read separately.
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
            var model = TryFit(listings, out var usableListings);

            return model ?? throw new InvalidOperationException(
                $"Need at least {MinimumTrainingListings} usable listings to fit a valuation model, found {usableListings}.");
        }

        /// <summary>
        /// The fitted model, or null when there are not enough usable listings for one.
        /// <paramref name="usableListings"/> is how many survived the quality filter.
        /// </summary>
        public static ValuationModel? TryFit(IEnumerable<PropertyListing> listings, out int usableListings)
        {
            var training = ListingQuality.UsableSubjects(listings, out var duplicatesCollapsed);

            usableListings = training.Count;

            return training.Count < MinimumTrainingListings
                ? null
                : new ValuationModel(training, duplicatesCollapsed);
        }

        /// <summary>The fewest usable listings a fit needs.</summary>
        public static int MinimumListings => MinimumTrainingListings;

        private ValuationModel(
            List<(ValuationSubject Subject, double LogPricePerM2)> training, int duplicatesCollapsed)
        {
            DuplicateListings = duplicatesCollapsed;

            var subjects = training.Select(x => x.Subject).ToList();

            // Where each market area actually is, so a property can be placed in its
            // municipality or district when its own zone is too thin to price.
            _geographyByArea = subjects
                .GroupBy(x => x.MarketAreaId)
                .ToDictionary(
                    g => g.Key,
                    g => (
                        District: g.Select(x => x.District).FirstOrDefault(x => !string.IsNullOrEmpty(x)) ?? string.Empty,
                        Municipality: g.Select(x => x.Municipality).FirstOrDefault(x => !string.IsNullOrEmpty(x)) ?? string.Empty));

            // Three nested location levels, each kept only where there is enough evidence to
            // support one. Over half the market areas hold fewer than MinimumListingsPerArea
            // listings; before this they got no column at all and were therefore priced as the
            // baseline area - so a quiet village in the interior was valued as though it sat in
            // whichever area happened to sort first. Falling back through municipality and then
            // district keeps those properties somewhere near the right part of the country.
            _catchAllAreas = CatchAllAreasIn(subjects);

            // A catch-all area is deliberately never dense, however many listings it holds: it is
            // not a place, so it must not get a price level of its own. Its listings still count
            // toward the municipality below, which is the most specific thing we honestly know
            // about them.
            _denseAreas = subjects
                .Where(x => !_catchAllAreas.Contains(x.MarketAreaId))
                .GroupBy(x => x.MarketAreaId)
                .Where(g => g.Count() >= MinimumListingsPerArea)
                .Select(g => g.Key)
                .ToHashSet();

            _denseMunicipalities = subjects
                .Where(x => !_denseAreas.Contains(x.MarketAreaId))
                .GroupBy(x => MunicipalityKeyOf(x))
                .Where(g => g.Key.Length > 0 && g.Count() >= MinimumListingsPerArea)
                .Select(g => g.Key)
                .ToHashSet();

            _denseDistricts = subjects
                .Where(x => !_denseAreas.Contains(x.MarketAreaId)
                         && !_denseMunicipalities.Contains(MunicipalityKeyOf(x)))
                .GroupBy(x => DistrictOf(x))
                .Where(g => g.Key.Length > 0 && g.Count() >= MinimumListingsPerArea)
                .Select(g => g.Key)
                .ToHashSet();

            // Skip(1) drops one level per dummy set - the baseline the rest are measured
            // against. Keep them all and the set equals the intercept, so the fit has no
            // unique answer - and it fails silently, as huge cancelling coefficients.
            _locationColumns = subjects
                .Select(x => EffectiveLocation(x))
                .Distinct()
                .OrderBy(x => x, StringComparer.Ordinal)
                .Skip(1)
                .ToList();

            _typologyColumns = subjects.Select(x => x.Typology).Distinct().OrderBy(x => x).Skip(1).ToList();
            _propertyTypeColumns = subjects.Select(x => x.PropertyType).Distinct().OrderBy(x => x).Skip(1).ToList();

            _medianFloor = Median(subjects.Where(x => x.Floor.HasValue).Select(x => (double)x.Floor!.Value));
            _medianBeachMeters = Median(subjects.Where(ValuationSubject.KnowsBeachDistance)
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

            // Bathrooms go into the fit raw rather than centred, so the model itself never needed
            // their average. A valuation does: what three bathrooms are worth is what they are
            // worth ABOVE what the market usually has, and crediting all three against zero would
            // pay every property a premium for being ordinary.
            _averageBathrooms = subjects.Average(x => (double)x.Bathrooms);

            var allRows = subjects.Select(x => BuildRow(x)).ToList();
            var allTargets = training.Select(x => x.LogPricePerM2).ToList();

            // Two passes. The first exists only to find the rows the rest of the data says are
            // wrong; the second is the fit we keep. ListingQuality rejects what is impossible on
            // its face - this catches what only looks wrong in company: a real price against a
            // swapped area, a whole building filed as a flat. Squared error means one such row
            // outweighs hundreds of ordinary ones, so leaving them in bends every coefficient.
            var keep = RowsWorthKeeping(allRows, allTargets);

            var rows = keep.Select(i => allRows[i]).ToList();
            var targets = keep.Select(i => allTargets[i]).ToList();

            subjects = keep.Select(i => subjects[i]).ToList();

            TrainingListings = subjects.Count;
            DiscardedListings = training.Count - subjects.Count;

            var fit = LeastSquares.Fit(rows, targets);
            _coefficients = fit.Coefficients;

            // The residual surface: where the regression is wrong, and where geographically.
            _residuals = new List<(double, double, double)>();

            // The same points, carrying the zone each was filed under rather than its error -
            // this is what lets a property's coordinates decide which zone it is in.
            _listingLocations = new List<(double, double, int)>();

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
                {
                    _residuals.Add(((double)subject.Latitude.Value, (double)subject.Longitude.Value, residual));

                    _listingLocations.Add((
                        (double)subject.Latitude.Value, (double)subject.Longitude.Value, subject.MarketAreaId));
                }
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

            // Where the property really is, rather than where its address said. Everything after
            // this prices the located area, so the zone the user picked can no longer move the
            // answer on its own.
            var locatedMarketAreaId = LocateMarketArea(subject.Latitude, subject.Longitude, subject.MarketAreaId);

            var logPricePerM2 = LeastSquares.Predict(_coefficients, BuildRow(subject, locatedMarketAreaId));

            var neighbours = NearestNeighbours(subject);
            var (correction, evidence) = NeighbourhoodCorrection(neighbours);

            logPricePerM2 += correction;

            return new ValuationPrediction
            {
                PricePerM2 = (decimal)Math.Exp(logPricePerM2),
                LocalComparablesUsed = neighbours.Count,
                NearestComparableMeters = neighbours.Count == 0 ? double.MaxValue : neighbours.Min(x => x.Distance),
                Spread = PredictionSpread * (1 + (ThinEvidenceWidening / (1 + evidence))),
                LocatedMarketAreaId = locatedMarketAreaId,
                LocatedByCoordinates = locatedMarketAreaId != subject.MarketAreaId,
            };
        }

        /// <summary>
        /// Which market area a property at these coordinates should be priced as. The coordinates
        /// win whenever there are listings near them; the stored area is only the fallback.
        ///
        /// The point is that a zone comes from a dropdown and can be mis-picked, while a
        /// coordinate cannot. Around Quarteira the same flat priced as "Centro - Quarteira Velha"
        /// and as the catch-all "Quarteira" differs by roughly 30%, and that catch-all overlaps
        /// every other zone in the town - so the least reliable field in the request was deciding
        /// the price, and the most reliable one was only trimming it by a few percent afterwards.
        ///
        /// Voted rather than taken from the single nearest listing, because one neighbour can
        /// itself be mis-filed. Votes fade with distance on the same kernel the neighbourhood
        /// correction uses, so a listing next door outweighs one at the edge of the search.
        /// </summary>
        public int LocateMarketArea(decimal? latitude, decimal? longitude, int storedMarketAreaId)
        {
            if (!latitude.HasValue || !longitude.HasValue)
                return storedMarketAreaId;

            var latitudeDegrees = (double)latitude.Value;
            var longitudeDegrees = (double)longitude.Value;

            var winner = _listingLocations
                .Select(x => (
                    x.MarketAreaId,
                    Distance: Calculator.CalculateDistanceMeters(
                        latitudeDegrees, longitudeDegrees, x.Latitude, x.Longitude)))
                // A catch-all area gets no vote. It usually holds more listings in a town centre
                // than any real zone does, so left in it wins nearly every vote - which replaced
                // one mis-picked zone with a worse one, and cost a Quarteira flat 30%.
                .Where(x => x.Distance <= LocationVoteMeters && !_catchAllAreas.Contains(x.MarketAreaId))
                .OrderBy(x => x.Distance)
                .Take(LocationVoteCount)
                .GroupBy(x => x.MarketAreaId)
                .Select(x => (MarketAreaId: x.Key, Weight: x.Sum(vote => NeighbourWeight(vote.Distance))))
                .OrderByDescending(x => x.Weight)
                .ThenBy(x => x.MarketAreaId)
                .FirstOrDefault();

            // No listing near enough to vote: the coordinates tell us nothing about which zone
            // this is, so the address is still the best answer we have.
            return winner.Weight <= 0 ? storedMarketAreaId : winner.MarketAreaId;
        }

        /// <summary>
        /// How much the neighbours say the regression is wrong here, on the log scale.
        ///
        /// Two things this must get right. A neighbour next door tells us far more than one at
        /// the edge of the search, so its say fades with distance; and when everything nearby is
        /// actually quite far, the correction has to fade toward zero rather than be applied in
        /// full - which is what used to happen, and it let a property with no real local evidence
        /// borrow the errors of a town 20km away at full strength.
        /// </summary>
        /// <returns>
        /// The correction to add, and how much local evidence stood behind it - the caller needs
        /// the second number to say how certain the answer is.
        /// </returns>
        private static (double Correction, double Evidence) NeighbourhoodCorrection(
            List<(double Distance, double Residual)> neighbours)
        {
            if (neighbours.Count == 0)
                return (0, 0);

            var weights = neighbours
                .Select(x => NeighbourWeight(x.Distance))
                .ToList();

            var evidence = weights.Sum();

            // A weighted median rather than a weighted mean: one absurd listing in the street
            // should not move the answer, and on this scale absurd listings are common.
            var correction = WeightedMedian(neighbours.Select(x => x.Residual).ToList(), weights);

            // Shrink toward "the regression was right" in proportion to how much the neighbours
            // are really worth. Ten neighbours on the doorstep barely move it; ten across the
            // county leave the regression almost untouched.
            return (correction * (evidence / (evidence + 1)), evidence);
        }

        /// <summary>
        /// How much a listing this far away is worth as evidence: full weight next door, half at
        /// <see cref="NeighbourKernelMeters"/>, fading from there. One kernel for both questions
        /// the neighbours are asked - how wrong the regression is here, and which zone this is -
        /// so the two can never disagree about what "nearby" means.
        /// </summary>
        internal static double NeighbourWeight(double metres)
        {
            return 1 / (1 + Math.Pow(metres / NeighbourKernelMeters, 2));
        }

        /// <summary>
        /// The value with half the weight below it and half above - the median, when some
        /// observations count more than others.
        /// </summary>
        private static double WeightedMedian(List<double> values, List<double> weights)
        {
            var ordered = values
                .Select((value, i) => (Value: value, Weight: weights[i]))
                .OrderBy(x => x.Value)
                .ToList();

            var half = ordered.Sum(x => x.Weight) / 2;
            var running = 0d;

            foreach (var (value, weight) in ordered)
            {
                running += weight;

                if (running >= half)
                    return value;
            }

            return ordered[^1].Value;
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
        /// The areas that are not really places. When the source does not know which neighbourhood
        /// a listing is in, it files it under a zone named after the whole town - so "Quarteira /
        /// Quarteira" holds stock from every corner of Quarteira and overlaps all 23 real zones in
        /// it. Measured on this data: those listings span 13m to 1.6km of the town centre, and the
        /// area's median is EUR 7,276/m2 against EUR 4,290 for the old town next door. Pricing a
        /// flat as that area prices it as the dearest average in town, wherever it actually is.
        ///
        /// Found from the data rather than named, because eleven towns here have one - Faro, with
        /// 76 real zones, and Setubal with 64 among them. Only ever treated as a catch-all when
        /// the town has other areas to fall back on: where it is all we have, it is a real place
        /// as far as we can tell.
        /// </summary>
        internal static HashSet<int> CatchAllAreasIn(List<ValuationSubject> subjects)
        {
            var townsWithRealZonesToo = subjects
                .GroupBy(x => Calculator.NormalizeText(x.Town))
                .Where(x => x.Key.Length > 0 && x.Select(subject => subject.MarketAreaId).Distinct().Count() > 1)
                .Select(x => x.Key)
                .ToHashSet();

            return subjects
                .Where(x => Calculator.NormalizeText(x.Zone) == Calculator.NormalizeText(x.Town)
                            && townsWithRealZonesToo.Contains(Calculator.NormalizeText(x.Town)))
                .Select(x => x.MarketAreaId)
                .ToHashSet();
        }

        /// <summary>
        /// The district a property sits in, taken from the property itself when it knows and
        /// otherwise looked up from its market area - an owned property carries only an area id.
        /// </summary>
        /// <param name="marketAreaId">
        /// Which area to look the geography up from, when it is not the one the subject carries -
        /// a property whose zone was decided by its coordinates. Null means "the stored one".
        /// Same meaning on <see cref="MunicipalityKeyOf"/>, <see cref="EffectiveLocation"/> and
        /// <see cref="BuildRow"/>.
        /// </param>
        private string DistrictOf(ValuationSubject subject, int? marketAreaId = null)
        {
            if (!string.IsNullOrEmpty(subject.District))
                return subject.District;

            return _geographyByArea.TryGetValue(marketAreaId ?? subject.MarketAreaId, out var geography)
                ? geography.District
                : string.Empty;
        }

        /// <summary>
        /// Municipality names repeat across districts, so the key has to carry both or two
        /// unrelated places thousands of a kilometre apart would share one price level.
        /// </summary>
        /// <inheritdoc cref="DistrictOf"/>
        private string MunicipalityKeyOf(ValuationSubject subject, int? marketAreaId = null)
        {
            var municipality = subject.Municipality;

            if (string.IsNullOrEmpty(municipality)
                && _geographyByArea.TryGetValue(marketAreaId ?? subject.MarketAreaId, out var geography))
                municipality = geography.Municipality;

            var district = DistrictOf(subject, marketAreaId);

            return string.IsNullOrEmpty(municipality) || string.IsNullOrEmpty(district)
                ? string.Empty
                : $"{district}|{municipality}";
        }

        /// <summary>
        /// The most specific place this property can actually be priced as. Falls from its own
        /// market area, through its municipality, to its district, and finally to the national
        /// baseline - each step only taken when the level above has too little data to stand on.
        /// </summary>
        /// <inheritdoc cref="DistrictOf"/>
        private string EffectiveLocation(ValuationSubject subject, int? marketAreaId = null)
        {
            var area = marketAreaId ?? subject.MarketAreaId;

            if (_denseAreas.Contains(area))
                return $"A:{area}";

            var municipality = MunicipalityKeyOf(subject, marketAreaId);

            if (_denseMunicipalities.Contains(municipality))
                return $"M:{municipality}";

            var district = DistrictOf(subject, marketAreaId);

            return _denseDistricts.Contains(district) ? $"D:{district}" : "national";
        }

        /// <summary>
        /// Turns a property into the row of numbers the coefficients multiply. The order here
        /// IS the model's column order - it must stay identical between fitting and predicting.
        ///
        /// The beach still enters as ln(metres), not as the yes/no "within 500m" the Feature
        /// Impact screen reports. That is deliberate: a smooth distance carries more information
        /// than a threshold, and this is where the € figure is decided, so nothing here is
        /// rounded off for the sake of a simpler headline.
        /// </summary>
        /// <inheritdoc cref="DistrictOf"/>
        private double[] BuildRow(ValuationSubject subject, int? marketAreaId = null)
        {
            var logArea = Math.Log(Math.Max(1, subject.AreaM2)) - _averageLogArea;
            var logBeach = Math.Log(BeachMetersOf(subject)) - _averageLogBeachMeters;

            var row = new List<double> { 1 };

            foreach (var (_, read) in BooleanFeatures)
            {
                row.Add(read(subject) ? 1 : 0);
            }

            // Needs-renovation is modelled here rather than in BooleanFeatures because it is read
            // off Condition, which also carries NewBuild and Renovated.
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

            var location = EffectiveLocation(subject, marketAreaId);

            foreach (var column in _locationColumns)
            {
                row.Add(location == column ? 1 : 0);
            }

            return row.ToArray();
        }

        /// <summary>
        /// Distance to the beach, falling back to the typical listing when unknown and never
        /// below 50m (the log of a near-zero distance would swamp everything else).
        /// </summary>
        private double BeachMetersOf(ValuationSubject subject)
        {
            // Treat nonsense the same as missing: fall back to the typical listing rather than
            // believing a number that came from a broken coordinate.
            if (!ValuationSubject.KnowsBeachDistance(subject))
                return Math.Max(50, _medianBeachMeters);

            return Math.Max(50, subject.DistanceToBeachMeters!.Value);
        }

        /// <summary>
        /// Which rows the second pass should fit on: everything within <see cref="OutlierSigmas"/>
        /// of a first, throwaway fit. Returns the indices to keep, in order.
        /// </summary>
        private static List<int> RowsWorthKeeping(List<double[]> rows, List<double> targets)
        {
            var exploratory = LeastSquares.Fit(rows, targets);

            var residuals = new double[rows.Count];

            for (var i = 0; i < rows.Count; i++)
            {
                residuals[i] = targets[i] - LeastSquares.Predict(exploratory.Coefficients, rows[i]);
            }

            // Median absolute deviation rather than the standard deviation: the rows we are
            // hunting are precisely the ones that would inflate a standard deviation and so
            // hide inside it. 1.4826 puts MAD back on the same scale as a normal sigma.
            var middle = Median(residuals);
            var scale = 1.4826 * Median(residuals.Select(x => Math.Abs(x - middle)));

            var everything = Enumerable.Range(0, rows.Count).ToList();

            // A fit with no scatter at all (the synthetic markets the unit tests build) has
            // nothing to trim, and a zero scale would otherwise reject every single row.
            if (scale <= 1e-9)
                return everything;

            var kept = everything
                .Where(i => Math.Abs(residuals[i] - middle) <= OutlierSigmas * scale)
                .ToList();

            // Never trim so hard that the refit has less to work with than it needs.
            return kept.Count > rows[0].Length ? kept : everything;
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
