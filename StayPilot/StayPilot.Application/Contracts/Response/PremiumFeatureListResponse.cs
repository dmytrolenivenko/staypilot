using StayPilot.Application.Contracts.Response.Base;

namespace StayPilot.Application.Contracts.Response
{
    /// <summary>
    /// Response that carries what every feature is worth market-wide.
    /// Sent back both when reading the stored values and after recalculating them.
    /// </summary>
    public class PremiumFeatureListResponse : ResponseBase
    {
        /// <summary>One entry per feature. Empty when the values have never been calculated.</summary>
        public List<PremiumFeatureResponse> Items { get; set; } = new();
    }
}
