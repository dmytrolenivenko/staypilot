
using StayPilot.Domain.Entities;

namespace StayPilot.Application.Interfaces.Repositories
{
    public interface IOwnedPropertyRepository
    {
        Task<OwnedProperty> CreateOwnedPropertyAsync(OwnedProperty entity);

        Task<OwnedProperty?> UpdateOwnedPropertyAsync(OwnedProperty entity);

        Task<string?> DeleteOwnedPropertyAsync(int id);

        Task<OwnedProperty?> GetOwnedPropertyAsync(int id);

        Task<List<OwnedProperty>> GetAllOwnedPropertyAsync();

        Task SaveChangesAsync();
    }
}
