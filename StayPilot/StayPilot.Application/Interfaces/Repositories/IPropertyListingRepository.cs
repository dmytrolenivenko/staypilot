using StayPilot.Domain.Entities;

namespace StayPilot.Application.Interfaces.Repositories
{
    public interface IPropertyListingRepository
    {
        Task<IEnumerable<PropertyListing>> GetAllPropertyListingsAsync();

        Task<PropertyListing?> GetPropertyListingByIdAsync(int id);

        Task<PropertyListing?> GetPropertyListingByUrlAsync(string url);

        Task<PropertyListing> AddPropertyListingAsync(PropertyListing propertyListing);

        Task SaveChangesAsync();
    }
}
