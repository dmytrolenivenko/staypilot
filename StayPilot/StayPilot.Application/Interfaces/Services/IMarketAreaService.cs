using StayPilot.Application.Contracts.Response;

namespace StayPilot.Application.Interfaces.Services
{
    public interface IMarketAreaService
    {
        Task<List<MarketAreaResponse>> GetAllMarketAreasAsync();
    }
}
