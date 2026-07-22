
using StayPilot.Domain.Entities;

namespace StayPilot.Application.Interfaces.Repositories
{
    public interface IOwnedPropertyRepository
    {
        Task<OwnedProperty> CreateOwnedPropertyAsync(OwnedProperty entity);

        // Fix: return type is now nullable, so we have a way to say
        // "there was no row with this Id to update".
        Task<OwnedProperty?> UpdateOwnedPropertyAsync(OwnedProperty entity);

        Task<string?> DeleteOwnedPropertyAsync(int id);

        Task<OwnedProperty?> GetOwnedPropertyAsync(int id);

        Task SaveChangesAsync();
    }
}
