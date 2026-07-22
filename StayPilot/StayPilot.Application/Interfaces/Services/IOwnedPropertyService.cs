
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Contracts.Request;

namespace StayPilot.Application.Interfaces.Services
{
    public interface IOwnedPropertyService
    {
        // Fix: this took a whole request before, but a "get by Id" only needs the Id.
        // Also returns null now, for when no row has this Id.
        Task<OwnedPropertyResponse?> GetOwnedPropertyAsync(int id);

        Task<OwnedPropertyResponse> AddOwnedPropertyAsync(OwnedPropertyRequest request);

        // Fix: the Id here was a string, but OwnedProperty.Id is an int.
        Task<string?> DeleteOwnedPropertyAsync(int id);

        // Fix: added the id, since OwnedPropertyRequest has no Id of its own,
        // so there was no way to know which row to update. Returns null now,
        // for when no row has this Id.
        Task<OwnedPropertyResponse?> UpdateOwnedPropertyAsync(int id, OwnedPropertyRequest request);
    }
}
