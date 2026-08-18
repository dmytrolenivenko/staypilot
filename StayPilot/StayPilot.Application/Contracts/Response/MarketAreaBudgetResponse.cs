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

        /// <summary>
        /// The budget after the stretch was applied - what a place actually had to come in under
        /// to appear here. Equal to <see cref="Budget"/> when no stretch was asked for.
        /// </summary>
        public decimal Reach { get; set; }

        public List<MarketAreaBudgetItemResponse> Items { get; set; } = new();

        /// <summary>When the numbers were last worked out. Null while the table is empty.</summary>
        public DateTime? CalculatedAtUtc { get; set; }
    }

    /// <summary>
    /// The best a budget reaches in one place.
    /// </summary>
    public class MarketAreaBudgetItemResponse
    {
        /// <summary>Which grain this row measures, so the screen can name the place properly.</summary>
        public AreaLevel Level { get; set; }

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

        /// <summary>
        /// True when this place is only within reach because the budget was stretched - the
        /// typology above usually sells here for more than the budget itself.
        /// </summary>
        public bool NeedsStretch { get; set; }

        /// <summary>
        /// Every typology the budget reaches here, most rooms first, not only the biggest one.
        ///
        /// The headline answer is "the most rooms your money buys", which quietly assumes more
        /// rooms is what you want. Often it is not: the same budget that reaches a small T3 here
        /// also reaches a large, cheaper-per-metre T2, and that trade is the actual decision.
        /// </summary>
        public List<MarketAreaBudgetTypologyResponse> AffordableTypologies { get; set; } = new();
    }

    /// <summary>
    /// One typology a budget reaches in one place. The same four numbers the headline row
    /// carries, so a row and its alternatives can be read against each other directly.
    /// </summary>
    public class MarketAreaBudgetTypologyResponse
    {
        public Typology Typology { get; set; }

        public decimal MedianPrice { get; set; }

        public decimal MedianAreaM2 { get; set; }

        public decimal MedianPricePerM2 { get; set; }

        public int ListingCount { get; set; }
    }
}
