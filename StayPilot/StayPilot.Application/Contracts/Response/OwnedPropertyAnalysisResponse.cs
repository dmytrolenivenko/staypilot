using StayPilot.Domain.Enums;
using StayPilot.Application.Contracts.Response.Base;
using StayPilot.Application.Contracts.Response.SubResponse;

namespace StayPilot.Application.Contracts.Response
{
    public class OwnedPropertyAnalysisResponse : ResponseBase
    {
        // headline (you already have these)
        public decimal MinPrice { get; set; }   // low end of the range
        public decimal MidPrice { get; set; }   // the headline "best guess" (median-based)
        public decimal MaxPrice { get; set; }   // high end
        public decimal AveragePrice { get; set; }  // mean-based price; show next to MidPrice — the gap flags a skewed market

        // confidence (the "how much to trust it" we discussed)
        public ValuationConfidence ConfidenceLevel { get; set; }  // High / Medium / Low  (new enum)
        public int CompsCount { get; set; }                       // how many comps the numbers below actually rest on
        public int ComparablesFound { get; set; }                 // how many the search turned up, before only the nearest were used

        // the math trail
        public decimal MarketRatePerM2 { get; set; }  // median €/m² across the comps
        public decimal EstimateBeforeAdjustments { get; set; }  // MarketRatePerM2 × your AreaM2, before Adjustments are applied

        // raw comp spread, for convenience (same info is in Comps, this just saves scanning it)
        public decimal MinCompPricePerM2 { get; set; }  // cheapest comp's €/m²

        public decimal MedianCompPricePerM2 { get; set; }  // median comp's €/m²
        public decimal MaxCompPricePerM2 { get; set; }  // priciest comp's €/m²
        public decimal AverageCompPricePerM2 { get; set; }  // mean comp's €/m² (vs the median above)

        public List<ValuationAdjustment> Adjustments { get; set; } = new List<ValuationAdjustment>();

        public List<ValuationComp> Comps { get; set; } = new List<ValuationComp>();  // the nearest few of CompsCount, for the table

        // where the price was taken from — the coordinates decide this, and they do not always
        // agree with the zone on the property. Shown so a surprising number can be traced to the
        // place it was priced as, instead of looking like the model changing its mind.
        public int LocatedMarketAreaId { get; set; }
        public string LocatedAreaName { get; set; } = string.Empty;
        public bool LocatedByCoordinates { get; set; }  // true = the coordinates overrode the stored zone

        public EquitySummary Equity { get; set; } = new EquitySummary();

    }
}
