
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
        /// <param name="district">
        /// Keep only places inside this district. Null or empty means the whole country.
        /// </param>
        /// <param name="municipality">
        /// Keep only places inside this município. Only meaningful at município or freguesia
        /// level - a district row has no município to match on, so passing one at district level
        /// returns nothing, which is the honest answer to a question that does not parse.
        /// </param>
        Task<List<MarketAreaStats>> GetLeaderboardAsync(
            AreaLevel level, int minListings, string? district = null, string? municipality = null);

        /// <summary>
        /// The same rows with their typology children loaded, for answering what a budget buys.
        /// Separate from <see cref="GetLeaderboardAsync"/> so the ordinary leaderboard read does
        /// not drag several thousand child rows along for nothing.
        /// </summary>
        Task<List<MarketAreaStats>> GetWithTypologiesAsync(
            AreaLevel level, int minListings, string? district = null, string? municipality = null);

        /// <summary>Adds the freshly worked out rows. Call SaveChanges after.</summary>
        Task AddMarketAreaStatsAsync(IEnumerable<MarketAreaStats> stats);

        /// <summary>Drops the rows from the previous run, so only one set is ever current.</summary>
        void RemoveMarketAreaStats(IEnumerable<MarketAreaStats> stats);

        Task SaveChangesAsync();
    }
}
