
using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;

namespace StayPilot.Application.Interfaces
{
    public interface IPropertyListingService
    {
        Task<PropertyListingResponse> AddPropertyListingAsync(PropertyListingRequest propertyListing);

        Task<PropertyListingResponse?> GetPropertyListingByIdAsync(int propertyId);
    }
}
