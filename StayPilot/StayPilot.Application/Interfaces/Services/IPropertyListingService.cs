using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Domain.Entities;

namespace StayPilot.Application.Interfaces.Services
{
    public interface IPropertyListingService
    {
        Task<PropertyListingResponse?> GetPropertyListingByIdAsync(int propertyId);

        Task<PropertyListingResponse> AddPropertyListingAsync(PropertyListingRequest propertyListing);

        Task<ListPropertyListingResponse> FilterPropertyAsync(ListPropertyListingRequest request);
    }
}
