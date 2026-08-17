using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;

namespace StayPilot.Application.Interfaces.Services
{
    /// <summary>
    /// Handles the price numbers per place: works them out, and reads them back ranked.
    /// </summary>
    public interface IMarketAreaStatsService
    {
        /// <summary>
        /// Works the stats out again from every listing we hold and replaces the whole table.
        /// Run it after an import: until it runs, the leaderboard shows the numbers from last time.
        /// </summary>
        Task<RecalculateMarketAreaStatsResponse> RecalculateMarketAreaStatsAsync();

        /// <summary>
        /// The most expensive or the cheapest places at the level asked for,
        /// with places below the sample gate left out.
        /// </summary>
        Task<MarketAreaLeaderboardResponse> GetLeaderboardAsync(MarketAreaLeaderboardRequest request);
    }
}
