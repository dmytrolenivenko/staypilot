using StayPilot.Application.Contracts.Response.SubResponse;
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Helpers.Calculators
{
    /// <summary>
    /// Valuing one of the user's own properties: the range around the estimate, how much to
    /// trust it, and what it has earned since purchase. Features live in
    /// <see cref="PremiumFeaturesCalculator"/>; plain statistics in <see cref="Calculator"/>.
    /// </summary>
    public static class OwnedPropertyValuationCalculator
    {
        private const double MediumConfidenceMeters = 5000;
        private const double HighConfidenceMeters = 1000;
        private const int HighConfidenceComparables = 10;

        /// <summary>Leap years included, so "years held" doesn't drift.</summary>
        private const decimal DaysPerYear = 365.25m;

        /// <summary>
        /// Low and high ends of an estimate, from the model's own error rather than the comp
        /// spread - tightly clustered comps don't make a valuation more certain.
        /// </summary>
        /// <param name="predictionSpread">The fit's log-scale error; e^spread either side.</param>
        public static (decimal MinPrice, decimal MaxPrice) PriceRange(decimal midPrice, double predictionSpread)
        {
            var spread = (decimal)Math.Exp(predictionSpread);

            return spread <= 0 ? (midPrice, midPrice) : (midPrice / spread, midPrice * spread);
        }

        /// <summary>
        /// How much to trust the estimate - judged on evidence near THIS property, not on the
        /// comp count. Listings only cover parts of the country, and a property outside them
        /// must not come back looking confident.
        /// </summary>
        public static ValuationConfidence DetermineConfidence(ValuationPrediction prediction)
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
        public static EquitySummary BuildEquity(decimal? purchasePrice, DateTime? purchaseDate, decimal currentEstimate)
        {
            var paid = purchasePrice ?? 0;
            var gainAmount = currentEstimate - paid;
            var gainPercent = paid > 0 ? gainAmount / paid * 100 : 0;

            // Fractional years (2.5, not 2) so the ROI maths is accurate.
            var yearsHeldExact = purchaseDate.HasValue
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
    }
}
