using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;

namespace StayPilot.Application.Interfaces.Services
{
    /// <summary>
    /// Handles the price numbers per place: works them out, and reads them back in the shapes
    /// the screens need.
    /// </summary>
    public interface IMarketAreaStatsService
    {
        /// <summary>
        /// Works the stats out again from every listing we hold and replaces the whole table.
        /// Run it after an import: until it runs, the screens show the numbers from last time.
        /// </summary>
        Task<RecalculateMarketAreaStatsResponse> RecalculateMarketAreaStatsAsync();

        /// <summary>
        /// Every place at the level asked for, with places below the sample gate left out.
        /// Unsorted as far as the caller cares - the browser ranks them.
        ///
        /// Carries the deal counts and the renovation numbers too, so the leaderboard, the deals
        /// column and the renovation screen all read from this one call.
        /// </summary>
        Task<MarketAreaLeaderboardResponse> GetLeaderboardAsync(MarketAreaLeaderboardRequest request);

        /// <summary>
        /// What a budget buys in each place: the most rooms it reaches and how much space that
        /// usually is. Places where the budget reaches nothing are left out.
        /// </summary>
        Task<MarketAreaBudgetResponse> GetBudgetRankingAsync(MarketAreaBudgetRequest request);

        /// <summary>
        /// Pairs of nearby places with a big price gap between them - where moving a few
        /// kilometres changes what a square meter costs.
        /// </summary>
        Task<MarketAreaNeighbourGapResponse> GetNeighbourGapsAsync(MarketAreaNeighbourGapRequest request);
    }
}
