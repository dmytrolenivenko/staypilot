
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

        /// <summary>When these numbers were last worked out (UTC time).</summary>
        public DateTime CalculatedAtUtc { get; set; }
    }
}
