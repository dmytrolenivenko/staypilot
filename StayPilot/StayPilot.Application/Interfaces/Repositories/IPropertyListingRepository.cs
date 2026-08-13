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
        Task<List<PropertyListing>?> GetBulkPropertyListingByUrlAsync(List<string> urls);


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
        /// Finds properties comparable to a given one: same property type, a room layout
        /// within one step, and either in the same market area or within radiusMeters of
        /// the given lat/lon. Only counts as fresh if its newest snapshot is not older
        /// than the cutoff. Returns at most 100, same market area first, then nearest.
        /// Falls back to the market area alone when the property has no coordinates.
        /// </summary>
        Task<List<PropertyListing>> GetComparablePropertyListingAsync(int marketId, PropertyType propertyType, Typology typology, int areaM2, decimal? latitude, decimal? longitude, int radiusMeters, int months);

        /// <summary>
        /// Gets every property listing across the whole dataset, with just its newest
        /// snapshot loaded. Used to calculate feature price premiums — no market area
        /// filter and no pagination, this is a bulk read for analysis, not a UI-facing list.
        /// </summary>
        Task<List<PropertyListing>> GetAllListingsForFeaturePremiumCalculationAsync();
    }
}
