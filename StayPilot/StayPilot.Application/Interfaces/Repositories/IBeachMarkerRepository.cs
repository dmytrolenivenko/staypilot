using StayPilot.Domain.Entities;

namespace StayPilot.Application.Interfaces.Repositories
{
    public interface IBeachMarkerRepository
    {
        Task<List<BeachMarker>> GetAllBeachMarkersAsync();
    }
}
