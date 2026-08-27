
using StayPilot.Application.Contracts.Response.Base;

namespace StayPilot.Application.Contracts.Response
{
    /// <summary>
    /// Response for the top deals: active listings ranked by how far below their own town's
    /// median euro per square meter they ask, best deal first.
    /// </summary>
    public class TopDealsResponse : ResponseBase
    {
        /// <summary>The ranked deals. Can be shorter than the requested count, or empty.</summary>
        public List<TopDealResponse> Items { get; set; } = new();

        /// <summary>
        /// When the market area stats behind this ranking were last worked out. Null while the
        /// stats table is still empty.
        /// </summary>
        public DateTime? CalculatedAtUtc { get; set; }
    }

    /// <summary>
    /// One listing found to be a deal, with the number that makes it one.
    /// </summary>
    public class TopDealResponse
    {
        /// <summary>The listing itself.</summary>
        public PropertyListingResponse Listing { get; set; } = null!;

        /// <summary>
        /// Median euro per square meter this listing was graded against: its own town's median
        /// for renovation projects, or for move-in-ready stock, whichever bucket it falls in.
        /// Never the blended median - mixing the two would call every fixer-upper a steal just
        /// for needing work.
        /// </summary>
        public decimal TownMedianPricePerM2 { get; set; }

        /// <summary>How far below that median this listing's price per square meter sits, in percent.</summary>
        public decimal DiscountPercent { get; set; }
    }
}
