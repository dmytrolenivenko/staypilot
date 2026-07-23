using JetBrains.Annotations;
using StayPilot.Application.Contracts.Request;
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;

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
        Task<(List<PropertyListing> Items, int TotalRecords)> FilterPropertyAsync(FilterPropertyListingRequest request);

        /// <summary>
        /// Write all pending changes to the database.
        /// </summary>
        Task SaveChangesAsync();

        /// <summary>
        /// Finds properties comparable to a given one: same market area, property type,
        /// typology, and a similar size (within 20% of areaM2). Only counts as fresh if
        /// its newest snapshot is not older than oldestAddUtc. Returns every match.
        /// </summary>
        Task<List<PropertyListing>> GetComparablePropertyListingAsync(int marketId, PropertyType propertyType, Typology typology, int areaM2, int months);
    }
}
