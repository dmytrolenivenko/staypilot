using JetBrains.Annotations;
using StayPilot.Application.Contracts.Request;
using StayPilot.Domain.Entities;

namespace StayPilot.Application.Interfaces.Repositories
{
    /// <summary>
    /// Reads and saves property listings in the database.
    /// </summary>
    public interface IPropertyListingRepository
    {
        /// <summary>
        /// Get one property by its Id. Returns null if it does not exist.
        /// </summary>
        Task<PropertyListing?> GetPropertyListingByIdAsync(int id);

        /// <summary>
        /// Get one property by its source URL. Used to check if it is already saved.
        /// Returns null if it does not exist.
        /// </summary>
        Task<PropertyListing?> GetPropertyListingByUrlAsync(string url);

        /// <summary>
        /// Add a new property. Call SaveChangesAsync to commit it.
        /// </summary>
        Task<PropertyListing> AddPropertyListingAsync(PropertyListing propertyListing);

        /// <summary>
        /// Search properties with filters, one page at a time.
        /// Returns the page of items and the total number of matches.
        /// </summary>
        Task<(List<PropertyListing> Items, int TotalRecords)> FilterPropertyAsync(ListPropertyListingRequest request);

        /// <summary>
        /// Write all pending changes to the database.
        /// </summary>
        Task SaveChangesAsync();
    }
}
