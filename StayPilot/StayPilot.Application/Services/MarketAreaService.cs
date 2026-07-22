using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Helpers.Mappers;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Application.Interfaces.Services;

namespace StayPilot.Infrastructure.Services
{
    public class MarketAreaService : IMarketAreaService
    {
        private readonly IMarketAreaRepository _marketAreaRepo;
        public MarketAreaService(IMarketAreaRepository marketAreaRepo)
        {
            _marketAreaRepo = marketAreaRepo;
        }

        public async Task<List<MarketAreaResponse>> GetAllMarketAreasAsync()
        {
            var marketAreas = await _marketAreaRepo.GetAllMarketAreasAsync();

            return marketAreas.Select(Converter.MapToResponse).ToList();
        }

        public async Task<List<string>> GetMarketAreaOptionsAsync(string? district, string? municipality, string? town)
        {
            return await _marketAreaRepo.GetMarketAreaOptionsAsync(district, municipality, town);
        }
    }
}
