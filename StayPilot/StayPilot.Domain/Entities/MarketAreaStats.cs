
using StayPilot.Domain.Enums;

namespace StayPilot.Domain.Entities
{
    /// <summary>
    /// Price numbers for one place, worked out from all the listings we have there.
    ///
    /// One row per place per level, so the same listing counts three times: once into its
    /// town row, once into its municipality row, once into its district row.
    ///
    /// Nothing here is typed in by hand. The whole table is rebuilt each time the stats are
    /// recalculated, the same way <see cref="PremiumFeature"/> is.
    /// </summary>
    public class MarketAreaStats
    {
        /// <summary>
        /// Database Id for this row.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Which level this row measures: a district, a municipality or a town.
        /// </summary>
        public AreaLevel Level { get; set; }

        /// <summary>
        /// District this row belongs to. Always filled.
        /// </summary>
        public string District { get; set; } = string.Empty;

        /// <summary>
        /// Municipality this row belongs to. Empty on a district row.
        /// </summary>
        public string Municipality { get; set; } = string.Empty;

        /// <summary>
        /// Town this row belongs to. Empty on a district or a municipality row.
        /// </summary>
        public string Town { get; set; } = string.Empty;

        /// <summary>
        /// How many listings the price below was worked out from.
        /// Read this before the price: a median taken from three listings is not a market.
        /// </summary>
        public int ListingCount { get; set; }

        /// <summary>
        /// Middle price for each square meter in this place.
        /// The middle value and not the average, so one very expensive villa cannot drag it up.
        /// </summary>
        public decimal MedianPricePerM2 { get; set; }

        /// <summary>
        /// When these numbers were last worked out (UTC time).
        /// </summary>
        public DateTime CalculatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
