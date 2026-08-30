
using StayPilot.Domain.Enums;

namespace StayPilot.Domain.Entities
{
    /// <summary>
    /// One owned property's valuation, as of the last recalculation.
    ///
    /// The portfolio list reads this table instead of refitting the pricing model and rescanning
    /// every listing on each page view. Recalculating overwrites the row for that property - one
    /// current valuation each, same trade already made for <see cref="PremiumFeature"/>: what is
    /// shown is a recalculation behind, not necessarily fresh.
    /// </summary>
    public class OwnedPropertyValuation
    {
        public int Id { get; set; }

        /// <summary>The property this valuation prices.</summary>
        public int OwnedPropertyId { get; set; }

        public OwnedProperty OwnedProperty { get; set; } = null!;

        public string District { get; set; } = string.Empty;

        public string Municipality { get; set; } = string.Empty;

        public string Town { get; set; } = string.Empty;

        /// <summary>The zone the model actually priced it as, which can differ from the above.</summary>
        public string LocatedAreaName { get; set; } = string.Empty;

        /// <summary>True when the coordinates decided the zone rather than the saved address.</summary>
        public bool LocatedByCoordinates { get; set; }

        public decimal MidPrice { get; set; }

        public decimal MinPrice { get; set; }

        public decimal MaxPrice { get; set; }

        public decimal PricePerM2 { get; set; }

        public ValuationConfidence ConfidenceLevel { get; set; }

        public string ConfidenceNote { get; set; } = string.Empty;

        /// <summary>AskSpreadSummary, serialized. Display-only - never filtered or sorted on.</summary>
        public string AskSpreadJson { get; set; } = string.Empty;

        /// <summary>AreaDemandResponse, serialized. Display-only - never filtered or sorted on.</summary>
        public string DemandJson { get; set; } = string.Empty;

        /// <summary>GrowthForecastResponse, serialized. Display-only - never filtered or sorted on.</summary>
        public string ForecastJson { get; set; } = string.Empty;

        /// <summary>When this row was calculated (UTC).</summary>
        public DateTime CalculatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
