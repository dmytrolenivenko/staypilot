using JetBrains.Annotations;
using StayPilot.Application.Contracts.Request;
using StayPilot.Domain.Entities;

namespace StayPilot.Application.Interfaces.Repositories
{
    public interface IPropertyListingRepository
    {
        Task<PropertyListing?> GetPropertyListingByIdAsync(int id);

        Task<PropertyListing?> GetPropertyListingByUrlAsync(string url);

        Task<PropertyListing> AddPropertyListingAsync(PropertyListing propertyListing);

        Task<(List<PropertyListing> Items, int TotalRecords)> FilterPropertyAsync(ListPropertyListingRequest request);

        Task SaveChangesAsync();
    }
}
