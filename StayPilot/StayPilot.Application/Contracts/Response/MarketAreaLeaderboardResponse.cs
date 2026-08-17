using StayPilot.Application.Contracts.Response.Base;

namespace StayPilot.Application.Contracts.Response
{
    /// <summary>
    /// Response for the leaderboard: the ranked places, priciest or cheapest first.
    /// </summary>
    public class MarketAreaLeaderboardResponse : ResponseBase
    {
        /// <summary>The ranked places. Empty when the stats have never been worked out.</summary>
        public List<MarketAreaStatsResponse> Items { get; set; } = new();

        /// <summary>
        /// When the numbers were last worked out. Null while the table is still empty.
        /// Worth showing: the stats are only as fresh as the last recalculation.
        /// </summary>
        public DateTime? CalculatedAtUtc { get; set; }
    }
}
