using StayPilot.Application.Contracts.Response.Base;

namespace StayPilot.Application.Contracts.Response
{
    /// <summary>
    /// Response for a recalculation: what the run actually did.
    /// </summary>
    public class RecalculateMarketAreaStatsResponse : ResponseBase
    {
        /// <summary>How many listings had a usable price and could be placed.</summary>
        public int ListingsUsed { get; set; }

        /// <summary>
        /// How many rows were written, counting all three levels together.
        /// </summary>
        public int RowsWritten { get; set; }

        /// <summary>When this run happened (UTC time).</summary>
        public DateTime CalculatedAtUtc { get; set; }
    }
}
