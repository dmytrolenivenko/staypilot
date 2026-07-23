using StayPilot.Domain.Enums;
using StayPilot.Application.Contracts.Response.SubResponse;

namespace StayPilot.Application.Contracts.Response
{
    public class OwnedPropertyAnalysisResponse
    {
        // headline (you already have these)
        public decimal MinPrice { get; set; }   // low end of the range
        public decimal MidPrice { get; set; }   // the headline "best guess"
        public decimal MaxPrice { get; set; }   // high end

        // confidence (the "how much to trust it" we discussed)
        public ValuationConfidence ConfidenceLevel { get; set; }  // High / Medium / Low  (new enum)
        public int CompsCount { get; set; }                       // how many comps backed it

        // the math trail
        public decimal MarketRatePerM2 { get; set; }  // median €/m² across the comps
        public decimal EstimateBeforeAdjustments { get; set; }  // MarketRatePerM2 × your AreaM2, before Adjustments are applied

        // raw comp spread, for convenience (same info is in Comps, this just saves scanning it)
        public decimal MinCompPricePerM2 { get; set; }  // cheapest comp's €/m²

        public decimal MedianCompPricePerM2 { get; set; }  // median comp's €/m²
        public decimal MaxCompPricePerM2 { get; set; }  // priciest comp's €/m²

        public List<ValuationAdjustment> Adjustments { get; set; } = new List<ValuationAdjustment>();

        public List<ValuationComp> Comps { get; set; } = new List<ValuationComp>();

        public EquitySummary Equity { get; set; } = new EquitySummary();

    }
}
