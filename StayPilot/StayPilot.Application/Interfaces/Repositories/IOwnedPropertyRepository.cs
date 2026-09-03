
using StayPilot.Domain.Entities;

namespace StayPilot.Application.Interfaces.Repositories
{
    public interface IOwnedPropertyRepository
    {
        Task<OwnedProperty> CreateOwnedPropertyAsync(OwnedProperty entity);

        Task<OwnedProperty?> UpdateOwnedPropertyAsync(OwnedProperty entity);

        Task<string?> DeleteOwnedPropertyAsync(int id, int ownerUserId);

        Task<OwnedProperty?> GetOwnedPropertyAsync(int id, int ownerUserId);

        Task<List<OwnedProperty>> GetAllOwnedPropertyAsync(int ownerUserId);

        /// <summary>Every cached valuation, keyed by OwnedPropertyId. A property with no entry
        /// here has never been valued.</summary>
        Task<Dictionary<int, OwnedPropertyValuation>> GetAllValuationsAsync();

        /// <summary>
        /// Saves the latest valuation for one property, replacing whatever was cached before.
        /// Staged only - call SaveChangesAsync to persist.
        /// </summary>
        Task UpsertValuationAsync(OwnedPropertyValuation valuation);

        Task SaveChangesAsync();
    }
}
