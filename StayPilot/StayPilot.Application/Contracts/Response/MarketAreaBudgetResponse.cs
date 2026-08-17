using StayPilot.Application.Contracts.Response.Base;
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Contracts.Response
{
    /// <summary>
    /// Response for what a budget buys: one row per place that has anything within reach.
    /// Places where nothing is affordable are left out rather than listed as blanks.
    /// </summary>
    public class MarketAreaBudgetResponse : ResponseBase
    {
        /// <summary>The budget these answers were worked out for, echoed back.</summary>
        public decimal Budget { get; set; }

        public List<MarketAreaBudgetItemResponse> Items { get; set; } = new();

        /// <summary>When the numbers were last worked out. Null while the table is empty.</summary>
        public DateTime? CalculatedAtUtc { get; set; }
    }

    /// <summary>
    /// The best a budget reaches in one place.
    /// </summary>
    public class MarketAreaBudgetItemResponse
    {
        public string DisplayName { get; set; } = string.Empty;

        public string District { get; set; } = string.Empty;

        public string Municipality { get; set; } = string.Empty;

        public string Town { get; set; } = string.Empty;

        /// <summary>
        /// The most rooms the budget reaches here, judged on what a typology usually costs in
        /// this place rather than on the cheapest advert in it.
        /// </summary>
        public Typology BestTypology { get; set; }

        /// <summary>What that typology usually costs here.</summary>
        public decimal MedianPrice { get; set; }

        /// <summary>How much space it usually has - the answer to "and how big is it".</summary>
        public decimal MedianAreaM2 { get; set; }

        /// <summary>Price for each square meter of that typology here.</summary>
        public decimal MedianPricePerM2 { get; set; }

        /// <summary>How many listings of that typology the numbers came from.</summary>
        public int TypologyListingCount { get; set; }

        /// <summary>How many listings the place has in total, all typologies.</summary>
        public int ListingCount { get; set; }
    }
}
