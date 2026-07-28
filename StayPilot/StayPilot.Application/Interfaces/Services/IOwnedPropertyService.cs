
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Contracts.Request;

namespace StayPilot.Application.Interfaces.Services
{
    public interface IOwnedPropertyService
    {
        Task<OwnedPropertyResponse?> GetOwnedPropertyAsync(int id);

        Task<OwnedPropertyResponse> AddOwnedPropertyAsync(OwnedPropertyRequest request);

        Task<string?> DeleteOwnedPropertyAsync(int id);

        Task<OwnedPropertyResponse?> UpdateOwnedPropertyAsync(int id, OwnedPropertyRequest request);

        Task<OwnedPropertyAnalysisResponse> EstimateOwnedPropertyValue(int id, int months);

        Task<List<OwnedPropertyResponse>> GetAllOwnedPropertiesAsync();

    }
}
