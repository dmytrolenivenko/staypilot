using StayPilot.Application.Contracts.Response;

namespace StayPilot.Application.Interfaces.Services
{
    public interface IMarketAreaService
    {
        Task<List<MarketAreaResponse>> GetAllMarketAreasAsync();

        Task<List<string>> GetMarketAreaOptionsAsync(string? district, string? municipality, string? town);
    }
}
