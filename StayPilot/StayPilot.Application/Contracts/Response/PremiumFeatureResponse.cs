using StayPilot.Domain.Enums;

namespace StayPilot.Application.Contracts.Response
{
    /// <summary>
    /// How much a single feature (for example a sea view) typically adds to the price.
    /// </summary>
    public class PremiumFeatureResponse
    {
        /// <summary>Which feature this is (for example "HasSeaView").</summary>
        public PremiumFeatures Feature { get; set; }

        /// <summary>
        /// Average price difference for having this feature, as a percentage. Read it together
        /// with <see cref="Basis"/>, which says what it is measured against when "if present"
        /// would mislead - "per bathroom", "within 500m of the beach".
        /// </summary>
        public decimal PremiumPercent { get; set; }

        /// <summary>Bottom of the 95% confidence range for <see cref="PremiumPercent"/>.</summary>
        public decimal LowerBoundPercent { get; set; }

        /// <summary>Top of the 95% confidence range for <see cref="PremiumPercent"/>.</summary>
        public decimal UpperBoundPercent { get; set; }

        /// <summary>
        /// False when the confidence range includes zero - the data cannot tell whether this
        /// feature affects price at all. Clients should say "no measurable effect" rather than
        /// print the headline percentage, which would read as a real (often negative) finding.
        /// </summary>
        public bool IsMeasurable { get; set; }

        /// <summary>How many listings the estimate was fitted on.</summary>
        public int SampleSize { get; set; }

        /// <summary>
        /// How many of those listings carry this feature AND had a comparable flat to be measured
        /// against - the real evidence behind the percentage. Clients should show this alongside
        /// <see cref="SampleSize"/> ("2,134 of 20,499"): the comparison size is identical on every
        /// row, so alone it says nothing about how well any one feature is measured.
        /// </summary>
        public int ListingsWithFeature { get; set; }

        /// <summary>
        /// The best this feature is worth under the conditions that favour it most - clients show
        /// it as "up to X%". Null for features whose worth does not vary, which is most of them;
        /// today the sea view (worth far more from the sand than from a sliver of horizon) and
        /// the lift (worth roughly twice as much from the third floor up) both carry one.
        ///
        /// Never display this without <see cref="MaximumBasis"/> next to it.
        /// </summary>
        public decimal? MaximumPercent { get; set; }

        /// <summary>
        /// The conditions <see cref="MaximumPercent"/> holds under, for example "within 500m of
        /// the beach". Never null when <see cref="MaximumPercent"/> is set.
        /// </summary>
        public string? MaximumBasis { get; set; }

        /// <summary>
        /// What the percentage is measured against, when "if present" would mislead. Null for
        /// ordinary yes/no features; clients should fall back to "if present" then.
        /// </summary>
        public string? Basis { get; set; }

        /// <summary>When this was last calculated (UTC time).</summary>
        public DateTime CalculatedAtUtc { get; set; }
    }
}
