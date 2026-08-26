
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
        /// (for example 12.5 means +12.5%). See <see cref="Basis"/> for what it is measured
        /// against when "if present" would mislead.
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
        /// How many of those listings carry this feature AND had a comparable flat to be measured
        /// against. This is the evidence behind the number - <see cref="SampleSize"/> is the same
        /// for every feature, so on its own it made a sea view read on 1,200 listings look as
        /// well-measured as a bathroom read on 4,800.
        /// </summary>
        public int ListingsWithFeature { get; set; }

        /// <summary>
        /// The best this feature is worth under the conditions that favour it most, when
        /// <see cref="PremiumPercent"/> averages over conditions that differ enormously. Null for
        /// features whose worth does not depend on anything else - most of them.
        ///
        /// Two have one. A sea view's headline averages beachfront views in with "sea view"
        /// adverts kilometres inland; this is the same fit read at the waterfront. A lift is
        /// worth roughly twice as much from the third floor up as it is on the ground, which one
        /// flat number hid entirely.
        /// </summary>
        public decimal? MaximumPercent { get; set; }

        /// <summary>
        /// The conditions <see cref="MaximumPercent"/> holds under, for example "within 500m of
        /// the beach". Never null when <see cref="MaximumPercent"/> is set - an "up to" with no
        /// stated conditions is a marketing claim rather than a measurement.
        /// </summary>
        public string? MaximumBasis { get; set; }

        /// <summary>
        /// What <see cref="PremiumPercent"/> is measured against, when "if present" would
        /// mislead - "per bathroom", "within 500m of the beach". Null for ordinary yes/no
        /// features, which is most of them.
        /// </summary>
        public string? Basis { get; set; }

        /// <summary>
        /// When this premium was last calculated (UTC time).
        /// </summary>
        public DateTime CalculatedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>Measurable only when the whole confidence range sits on one side of zero.</summary>
        public bool IsMeasurable => LowerBoundPercent > 0 || UpperBoundPercent < 0;
    }
}
