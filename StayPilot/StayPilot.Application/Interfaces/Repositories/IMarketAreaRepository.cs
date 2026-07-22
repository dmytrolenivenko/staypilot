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
        /// </summary>
        Task<List<MarketArea>> GetAllMarketAreasAsync();

        /// <summary>
        /// Get the list of next-level choices for the region picker.
        /// Each filter you pass narrows the result: give a district to get its
        /// towns, give district + municipality to get its zones, and so on.
        /// </summary>
        Task<List<string>> GetMarketAreaOptionsAsync(string? distrinct, string? municipality, string? town);
    }
}
