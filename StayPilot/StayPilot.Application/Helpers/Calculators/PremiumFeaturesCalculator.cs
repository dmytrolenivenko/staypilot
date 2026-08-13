using System.Text.RegularExpressions;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Contracts.Response.SubResponse;
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Helpers.Calculators
{
    /// <summary>
    /// What the premium features are worth - to the market, and to one property. The only way
    /// into the maths; the model behind it is internal. Fitting is expensive: fit once, reuse.
    /// </summary>
    public class PremiumFeaturesCalculator
    {
        /// <summary>Below this many units from typical, a counted feature gets no row.</summary>
        private const decimal MinimumUnitsWorthReporting = 0.05m;

        private readonly ValuationModel _model;

        private PremiumFeaturesCalculator(ValuationModel model)
        {
            _model = model;
        }

        /// <exception cref="InvalidOperationException">Not enough usable listings to fit.</exception>
        public static PremiumFeaturesCalculator Fit(IEnumerable<PropertyListing> listings)
        {
            return new PremiumFeaturesCalculator(ValuationModel.Fit(listings));
        }

        /// <summary>What each feature is worth market-wide, with confidence ranges.</summary>
        public IReadOnlyList<FeatureEffect> FeatureEffects => _model.FeatureEffects;

        /// <summary>How many listings the fit learned from.</summary>
        public int TrainingListings => _model.TrainingListings;

        /// <summary>The fit's typical error on the log scale. Build price ranges from this.</summary>
        public double PredictionSpread => _model.PredictionSpread;

        /// <summary>Prices a property per m², including the neighbourhood correction.</summary>
        public ValuationPrediction PredictPricePerM2(OwnedPropertyResponse property)
        {
            return _model.PredictPricePerM2(ValuationSubject.FromOwnedProperty(property));
        }

        /// <summary>
        /// How much of <paramref name="estimatedPrice"/> each feature accounts for. Yes/no
        /// features credit in full; counted ones only for what's ABOVE typical, else every
        /// property gets paid for being ordinary. Amounts compound, so they don't sum.
        /// </summary>
        public List<ValuationAdjustment> BuildAdjustments(OwnedPropertyResponse property, decimal estimatedPrice)
        {
            var subject = ValuationSubject.FromOwnedProperty(property);
            var adjustments = new List<ValuationAdjustment>();

            foreach (var effect in _model.FeatureEffects)
            {
                var adjustment = CountedUnitsFor(subject, effect.Feature) is { } counted
                    ? CountedAdjustment(subject, effect, counted, estimatedPrice)
                    : YesNoAdjustment(subject, effect, estimatedPrice);

                if (adjustment is not null)
                    adjustments.Add(adjustment);
            }

            return adjustments;
        }

        /// <summary>A "has it" row, or null when this property doesn't.</summary>
        private ValuationAdjustment? YesNoAdjustment(
            ValuationSubject subject, FeatureEffect effect, decimal estimatedPrice)
        {
            if (!ValuationModel.HasFeature(subject, effect.Feature))
                return null;

            return new ValuationAdjustment
            {
                Label = FriendlyFeatureName(effect.Feature),
                Amount = AmountFor(estimatedPrice, PercentFor(subject, effect), units: 1),
                IsMeasurable = effect.IsMeasurable,
                Detail = null,
            };
        }

        /// <summary>A counted row, or null when the property sits at the market's typical.</summary>
        private ValuationAdjustment? CountedAdjustment(
            ValuationSubject subject, FeatureEffect effect, CountedFeature counted, decimal estimatedPrice)
        {
            var unitsAboveTypical = counted.Units - counted.TypicalUnits;

            if (Math.Abs(unitsAboveTypical) < MinimumUnitsWorthReporting)
                return null;

            return new ValuationAdjustment
            {
                Label = FriendlyFeatureName(effect.Feature),
                Amount = AmountFor(estimatedPrice, PercentFor(subject, effect), unitsAboveTypical),
                IsMeasurable = effect.IsMeasurable,
                Detail = counted.Detail,
            };
        }

        /// <summary>
        /// The share of the price owed to a feature: what disappears when its multiplier is
        /// divided back out. Negative units fall out as a discount from the same formula.
        /// </summary>
        private static decimal AmountFor(decimal estimatedPrice, decimal percent, decimal units)
        {
            var multiplier = Math.Pow(1 + (double)percent / 100, (double)units);

            // A degenerate coefficient can zero the multiplier; dividing by it would throw.
            if (multiplier <= 0 || double.IsNaN(multiplier) || double.IsInfinity(multiplier))
                return 0;

            return Math.Round(estimatedPrice * (1 - 1 / (decimal)multiplier), 0);
        }

        /// <summary>
        /// What a feature is worth to THIS property. Only the sea view differs: the model
        /// carries a SeaView x distance term, so a beachfront flat credited with the market
        /// average would be badly undersold.
        /// </summary>
        private decimal PercentFor(ValuationSubject subject, FeatureEffect effect)
        {
            if (effect.Feature != PremiumFeatures.HasSeaView || subject.DistanceToBeachMeters is null)
                return effect.Percent;

            return _model.SeaViewPercentAt(subject.DistanceToBeachMeters.Value);
        }

        /// <summary>
        /// How much of a counted feature this property has vs the market. Null for yes/no
        /// features - that's what tells <see cref="BuildAdjustments"/> which row to build.
        /// </summary>
        private CountedFeature? CountedUnitsFor(ValuationSubject subject, PremiumFeatures feature)
        {
            switch (feature)
            {
                case PremiumFeatures.ExtraBathroom:
                    return CountedFeature.Of(subject.Bathrooms, _model.MarketAverageBathrooms);

                case PremiumFeatures.HasBalcony:
                    return CountedFeature.Of(subject.BalconyCount, _model.MarketAverageBalconies);

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

                case PremiumFeatures.BeachProximity:
                    return BeachProximityUnits(subject);

                default:
                    return null;
            }
        }

        /// <summary>
        /// Beach proximity in its own unit: halvings of distance. 500m against a 2km market
        /// median is two halvings, so the premium is earned twice.
        /// </summary>
        private CountedFeature? BeachProximityUnits(ValuationSubject subject)
        {
            var typical = _model.MarketMedianBeachMeters;

            if (subject.DistanceToBeachMeters is not > 0 || typical <= 0)
                return null;

            return new CountedFeature(
                (decimal)Math.Log2(typical / subject.DistanceToBeachMeters.Value),
                TypicalUnits: 0,
                $"{subject.DistanceToBeachMeters.Value:N0}m vs {typical:N0}m typical");
        }

        /// <summary>HasSeaView -> "Sea View", IsNewBuild -> "New Build".</summary>
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
}
