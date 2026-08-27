using StayPilot.Application.Contracts.Request;
using StayPilot.Domain.Entities;

namespace StayPilot.Application.Interfaces.Repositories
{
    /// <summary>
    /// Reads market area (region) data from the database.
    /// </summary>
    public interface IMarketAreaRepository
    {
        /// <summary>
        /// Get every market area saved in the database.
        /// Used when we need the whole table in memory (matching an address to an area).
        /// Screens that only show market areas should use <see cref="GetMarketAreasPageAsync"/>.
        /// </summary>
        Task<List<MarketArea>> GetAllMarketAreasAsync();

        /// <summary>
        /// Get market areas saved in the database, one page at a time.
        /// Returns the page of items and the total number of matches.
        /// </summary>
        Task<(List<MarketArea> Items, int TotalRecords)> GetMarketAreasPageAsync(MarketAreaRequest request);

        /// <summary>
        /// Get the list of next-level choices for the region picker.
        /// Each filter you pass narrows the result: give a district to get its
        /// towns, give district + municipality to get its zones, and so on.
        /// </summary>
        Task<List<string>> GetMarketAreaOptionsAsync(string? distrinct, string? municipality, string? town);
    }
}
