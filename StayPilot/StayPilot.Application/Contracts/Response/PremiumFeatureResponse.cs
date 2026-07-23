namespace StayPilot.Application.Contracts.Response
{
    /// <summary>
    /// How much a single feature (for example a sea view) typically adds to the price.
    /// </summary>
    public class PremiumFeatureResponse
    {
        /// <summary>Which feature this is (for example "HasSeaView").</summary>
        public string Feature { get; set; } = string.Empty;

        /// <summary>Average price difference for having this feature, as a percentage.</summary>
        public decimal PremiumPercent { get; set; }

        /// <summary>When this was last calculated (UTC time).</summary>
        public DateTime CalculatedAtUtc { get; set; }
    }
}
