
using StayPilot.Application.Contracts.Response;

namespace StayPilot.Application.Interfaces
{
    public interface IMarketAreaService
    {
        Task<List<MarketAreaResponse>> GetAllMarketAreasAsync();
    }
}
