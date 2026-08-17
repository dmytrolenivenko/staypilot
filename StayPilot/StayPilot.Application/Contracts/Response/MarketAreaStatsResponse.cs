
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Contracts.Response
{
    /// <summary>
    /// Response that carries the price numbers for one place.
    /// </summary>
    public class MarketAreaStatsResponse
    {
        /// <summary>Which level this row measures: a district, a municipality or a town.</summary>
        public AreaLevel Level { get; set; }

        public string District { get; set; } = string.Empty;

        /// <summary>Empty on a district row.</summary>
        public string Municipality { get; set; } = string.Empty;

        /// <summary>Empty on a district or a municipality row.</summary>
        public string Town { get; set; } = string.Empty;

        /// <summary>
        /// The place written out for a human: "Albufeira (Faro)".
        /// Built here so every screen names a place the same way.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// How many listings the price below was worked out from.
        /// Show it next to the price: a median from three listings is not a market.
        /// </summary>
        public int ListingCount { get; set; }

        /// <summary>Middle price for each square meter in this place.</summary>
        public decimal MedianPricePerM2 { get; set; }

        /// <summary>Middle floor area here, so you can see what kind of stock this place is.</summary>
        public decimal MedianAreaM2 { get; set; }

        /// <summary>
        /// How many listings here are asking clearly less than the model thinks they are worth.
        /// Under-priced, not cheap: a whole town being inexpensive is not a bargain.
        /// </summary>
        public int BelowEstimateCount { get; set; }

        /// <summary>How many listings here look like renovation projects.</summary>
        public int ProjectCount { get; set; }

        /// <summary>
        /// Middle price for each square meter of the project stock. Null when there is too little.
        /// </summary>
        public decimal? ProjectMedianPricePerM2 { get; set; }

        /// <summary>How many listings here are ready to move into.</summary>
        public int MoveInCount { get; set; }

        /// <inheritdoc cref="ProjectMedianPricePerM2"/>
        public decimal? MoveInMedianPricePerM2 { get; set; }

        /// <summary>
        /// How much cheaper a square meter is if it needs work, in euros. Positive means projects
        /// cost less than finished places, which is the normal case and the size of the discount
        /// you would be paid for taking the work on.
        ///
        /// Null when either side is missing. Compare it against a renovation cost yourself - this
        /// is measured from real adverts, and any build cost is an estimate, so the two should
        /// never be quietly subtracted into one number.
        /// </summary>
        public decimal? RenovationDiscountPerM2 { get; set; }

        /// <summary>When these numbers were last worked out (UTC time).</summary>
        public DateTime CalculatedAtUtc { get; set; }
    }
}
