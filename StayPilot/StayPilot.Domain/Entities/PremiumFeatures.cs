
namespace StayPilot.Domain.Entities
{
    /// <summary>
    /// The average price premium for having a specific feature (for example a sea view
    /// or a garage), calculated once across the whole market and reused afterward.
    /// </summary>
    public class PremiumFeatures
    {
        public int Id { get; set; }

        /// <summary>
        /// Which feature this premium is for (for example "HasSeaView", "HasGarage").
        /// Matches a boolean property name on PropertyListing/OwnedProperty.
        /// </summary>
        public string Feature { get; set; } = string.Empty;

        /// <summary>
        /// Average price difference for having this feature, as a percentage
        /// (for example 12.5 means +12.5%).
        /// </summary>
        public decimal PremiumPercent { get; set; }

        /// <summary>
        /// When this premium was last calculated (UTC time).
        /// </summary>
        public DateTime CalculatedAtUtc { get; set; }
    }
}
