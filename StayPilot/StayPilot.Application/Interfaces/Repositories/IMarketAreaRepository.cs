using StayPilot.Domain.Entities;

namespace StayPilot.Application.Interfaces.Repositories
{
    public interface IMarketAreaRepository
    {
        Task<List<MarketArea>> GetAllMarketAreasAsync();
    }
}
