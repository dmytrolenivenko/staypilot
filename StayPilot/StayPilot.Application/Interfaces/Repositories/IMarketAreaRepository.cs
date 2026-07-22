using StayPilot.Domain.Entities;

namespace StayPilot.Application.Interfaces.Repositories
{
    public interface IMarketAreaRepository
    {
        Task<List<MarketArea>> GetAllMarketAreasAsync();

        Task<List<string>> GetMarketAreaOptionsAsync(string? distrinct, string? municipality, string? town);
    }
}
