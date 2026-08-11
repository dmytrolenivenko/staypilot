using StayPilot.Application.Contracts.Request;
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
        public async Task<MarketAreaListResponse> GetMarketAreasPageAsync(MarketAreaRequest request)
        {
            // Ask the database for this page of market areas and the total number of matches.
            var (items, totalRecords) = await _marketAreaRepo.GetMarketAreasPageAsync(request);

            // Turn each market area entity into the response we send back,
            // then add the paging info so the caller knows how many pages exist.
            return new MarketAreaListResponse
            {
                Items = items.Select(Converter.MapToResponse).ToList(),
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalRecords = totalRecords
            };
        }

        /// <inheritdoc/>
        public async Task<List<string>> GetMarketAreaOptionsAsync(string? district, string? municipality, string? town)
        {
            return await _marketAreaRepo.GetMarketAreaOptionsAsync(district, municipality, town);
        }
    }
}
