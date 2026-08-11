
using StayPilot.Domain.Enums;

namespace StayPilot.Domain.Entities
{
    /// <summary>
    /// The average price premium for having a specific feature (for example a sea view
    /// or a garage), calculated once across the whole market and reused afterward.
    /// </summary>
    public class PremiumFeature
    {
        public int Id { get; set; }

        /// <summary>
        /// Which feature this premium is for (for example "HasSeaView", "HasGarage").
        /// Matches a boolean property name on PropertyListing/OwnedProperty.
        /// </summary>
        public PremiumFeatures Feature { get; set; }

        /// <summary>
        /// Average price difference for having this feature, as a percentage
        /// (for example 12.5 means +12.5%).
        ///
        /// For <see cref="PremiumFeatures.BeachProximity"/> this is not a yes/no premium - it is
        /// the gain per halving of the distance to the beach.
        /// </summary>
        public decimal PremiumPercent { get; set; }

        /// <summary>
        /// Bottom of the 95% confidence range for <see cref="PremiumPercent"/>. Read together
        /// with <see cref="UpperBoundPercent"/>: if the two straddle zero, the data cannot tell
        /// us whether this feature is worth anything, and the headline number is not a finding.
        /// </summary>
        public decimal LowerBoundPercent { get; set; }

        /// <summary>Top of the 95% confidence range for <see cref="PremiumPercent"/>.</summary>
        public decimal UpperBoundPercent { get; set; }

        /// <summary>How many listings the estimate was fitted on.</summary>
        public int SampleSize { get; set; }

        /// <summary>
        /// What <see cref="PremiumPercent"/> is measured against, when "if present" would
        /// mislead - beach proximity is per halving of distance, and a sea view is worth far
        /// more on the beachfront than inland. Null for ordinary yes/no features.
        /// </summary>
        public string? Basis { get; set; }

        /// <summary>
        /// When this premium was last calculated (UTC time).
        /// </summary>
        public DateTime CalculatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
