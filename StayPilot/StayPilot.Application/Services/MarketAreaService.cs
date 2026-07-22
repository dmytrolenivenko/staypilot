using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Helpers.Mappers;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Application.Interfaces.Services;

namespace StayPilot.Application.Services
{
    /// <summary>
    /// Handles market areas (regions). Reads them from the repository and
    /// turns them into the shape we send back.
    /// </summary>
    public class MarketAreaService : IMarketAreaService
    {
        private readonly IMarketAreaRepository _marketAreaRepo;
        public MarketAreaService(IMarketAreaRepository marketAreaRepo)
        {
            _marketAreaRepo = marketAreaRepo;
        }

        /// <inheritdoc/>
        public async Task<List<MarketAreaResponse>> GetAllMarketAreasAsync()
        {
            var marketAreas = await _marketAreaRepo.GetAllMarketAreasAsync();

            // Turn each market area entity into the response we send back.
            return marketAreas.Select(Converter.MapToResponse).ToList();
        }

        /// <inheritdoc/>
        public async Task<List<string>> GetMarketAreaOptionsAsync(string? district, string? municipality, string? town)
        {
            return await _marketAreaRepo.GetMarketAreaOptionsAsync(district, municipality, town);
        }
    }
}
