using StayPilot.Domain.Entities;

namespace StayPilot.Application.Interfaces.Repositories
{
    /// <summary>
    /// Reads beach data from the database.
    /// </summary>
    public interface IBeachMarkerRepository
    {
        /// <summary>
        /// Get every beach saved in the database.
        /// </summary>
        Task<List<BeachMarker>> GetAllBeachMarkersAsync();
    }
}
