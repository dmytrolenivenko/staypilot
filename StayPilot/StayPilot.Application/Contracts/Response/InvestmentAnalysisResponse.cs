
using StayPilot.Application.Contracts.Response.Base;
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Contracts.Response
{
    /// <summary>
    /// What it would take to buy, renovate and resell one property listing, worked out entirely
    /// from numbers this service computed. <see cref="Narrative"/> is the only field an AI ever
    /// touches, and only to describe the numbers below it — never to produce one of its own.
    /// </summary>
    public class InvestmentAnalysisResponse : ResponseBase
    {
        /// <summary>Set when this analysis is for a scraped listing. Null for an owned property.</summary>
        public int? PropertyListingId { get; set; }

        /// <summary>Set when this analysis is for one of the user's own properties. Null for a listing.</summary>
        public int? OwnedPropertyId { get; set; }

        /// <summary>The listing's ask price, or the property's purchase price when analyzing an owned property.</summary>
        public decimal AskPrice { get; set; }

        public int AreaM2 { get; set; }

        public PropertyCondition Condition { get; set; }

        public string District { get; set; } = string.Empty;

        public string Municipality { get; set; } = string.Empty;

        public string Town { get; set; } = string.Empty;

        /// <summary>The town's move-in-ready median €/m², the price basis behind <see cref="EstimatedResaleValue"/>.</summary>
        public decimal TownMoveInMedianPricePerM2 { get; set; }

        /// <summary>How many move-in-ready comps that median rests on. Drives <see cref="Confidence"/>.</summary>
        public int TownMoveInListingCount { get; set; }

        /// <summary>What renovating this property to move-in condition would cost. Calculated from today's build rates, or the caller's own estimate — see <see cref="RenovationCostIsOverride"/>.</summary>
        public decimal EstimatedRenovationCost { get; set; }

        /// <summary>True when <see cref="EstimatedRenovationCost"/> is the caller's own number, not the one calculated from build rates. Real repair costs vary too much (self-sourced materials, no labor hired) for one formula to fit everyone.</summary>
        public bool RenovationCostIsOverride { get; set; }

        /// <summary>
        /// Fixed-price renovation scopes to choose from before typing a custom number — Cosmetic,
        /// Full renovation, Full rebuild — priced for this property's area. See
        /// <see cref="Helpers.Calculators.InvestmentAnalysisCalculator.GetRenovationScopeOptions"/>.
        /// </summary>
        public List<BuildCostOption> RenovationOptions { get; set; } = [];

        /// <summary>What the property would sell for once move-in ready.</summary>
        public decimal EstimatedResaleValue { get; set; }

        /// <summary>Ask price plus renovation cost — everything that has to go in.</summary>
        public decimal TotalInvestment { get; set; }

        /// <summary>Resale value minus total investment. Negative is a loss.</summary>
        public decimal EstimatedProfit { get; set; }

        public decimal ProfitMarginPercent { get; set; }

        /// <summary>How much to trust these numbers, judged on how thin the town's comp count is.</summary>
        public ValuationConfidence Confidence { get; set; }

        public DateTime CalculatedAtUtc { get; set; }

        /// <summary>
        /// The AI-written investment thesis. Null until the narrative call is wired in, or
        /// whenever that call fails — the numbers above are always returned regardless.
        /// </summary>
        public string? Narrative { get; set; }
    }
}
