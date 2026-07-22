using StayPilot.Application.Contracts.Response;

namespace StayPilot.Application.Interfaces.Services
{
    /// <summary>
    /// Handles market areas (regions) for the caller.
    /// </summary>
    public interface IMarketAreaService
    {
        /// <summary>
        /// Get every market area in the shape we send back.
        /// </summary>
        Task<List<MarketAreaResponse>> GetAllMarketAreasAsync();

        /// <summary>
        /// Get the next-level choices for the region picker.
        /// Each filter you pass narrows the result (district, then municipality, then town).
        /// </summary>
        Task<List<string>> GetMarketAreaOptionsAsync(string? district, string? municipality, string? town);
    }
}
