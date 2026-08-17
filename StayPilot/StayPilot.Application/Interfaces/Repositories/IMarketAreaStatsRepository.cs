
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Interfaces.Repositories
{
    /// <summary>
    /// Reads and writes the market area stats table.
    /// The table is rebuilt from scratch on every recalculation, never patched row by row.
    /// </summary>
    public interface IMarketAreaStatsRepository
    {
        /// <summary>Every stats row we have, all levels.</summary>
        Task<List<MarketAreaStats>> GetAllMarketAreaStatsAsync();

        /// <summary>
        /// Every place at one level that clears the sample gate. A few hundred rows at most,
        /// so they all go back at once and the browser does the ranking.
        /// </summary>
        /// <param name="level">Which grain to return: districts, municipalities or towns.</param>
        /// <param name="minListings">
        /// Skip places with fewer listings than this. Thin places are still saved in the table,
        /// they are just left out of a ranking where they would read as a real finding.
        /// </param>
        Task<List<MarketAreaStats>> GetLeaderboardAsync(AreaLevel level, int minListings);

        /// <summary>Adds the freshly worked out rows. Call SaveChanges after.</summary>
        Task AddMarketAreaStatsAsync(IEnumerable<MarketAreaStats> stats);

        /// <summary>Drops the rows from the previous run, so only one set is ever current.</summary>
        void RemoveMarketAreaStats(IEnumerable<MarketAreaStats> stats);

        Task SaveChangesAsync();
    }
}
