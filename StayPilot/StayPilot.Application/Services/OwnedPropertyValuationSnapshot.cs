using StayPilot.Application.Contracts.Response.SubResponse;
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Services
{
    /// <summary>
    /// Everything a revaluation computes for one property that is not already sitting on the
    /// OwnedProperty row itself (name, type, typology, area). This is what gets serialized into
    /// <c>OwnedPropertyValuation.ResultJson</c> - not a wire contract, just the cache's own shape.
    ///
    /// Purchase price/date are deliberately left out: <see cref="AskSpreadSummary"/> is rebuilt
    /// from the live OwnedProperty row on every read instead of cached, so editing the purchase
    /// price does not require a revaluation just to stop the spread from lying.
    /// </summary>
    public class OwnedPropertyValuationSnapshot
    {
        public string District { get; set; } = string.Empty;
        public string Municipality { get; set; } = string.Empty;
        public string Town { get; set; } = string.Empty;
        public string LocatedAreaName { get; set; } = string.Empty;
        public bool LocatedByCoordinates { get; set; }

        public decimal MidPrice { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public decimal PricePerM2 { get; set; }

        public ValuationConfidence ConfidenceLevel { get; set; }
        public string ConfidenceNote { get; set; } = string.Empty;

        public AreaDemandResponse Demand { get; set; } = new();
        public GrowthForecastResponse Forecast { get; set; } = new();
    }
}
