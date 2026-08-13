using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Domain.Entities;

namespace StayPilot.Application.Interfaces.Services
{
    /// <summary>
    /// Handles property listings for the caller.
    /// </summary>
    public interface IPropertyListingService
    {
        /// <summary>
        /// Get one property by its Id. Returns null if it does not exist.
        /// </summary>
        Task<PropertyListingResponse?> GetPropertyListingByIdAsync(int propertyId);

        /// <summary> 
        /// Bulk add property listings that is going to use Add listing property 
        /// </summary>
        Task<BulkAddPropertyListingResponse> BulkAddPropertyListingAsync(BulkAddPropertyListingRequest request);

        /// <summary>
        /// Save a new property. If it already exists (same URL), the existing one is returned.
        /// Also sets its market area and its closest beach.
        /// </summary>
        Task<PropertyListingResponse> AddPropertyListingAsync(PropertyListingRequest propertyListing, List<MarketArea> marketAreasRepo, List<BeachMarker> beachesRepo, Dictionary<string, PropertyListing> existingListingsRepo);

        /// <summary>
        /// Search properties with filters, one page at a time.
        /// Returns the page of items and the total number of matches.
        /// </summary>
        Task<FilterPropertyListingResponse> FilterPropertyListingAsync(FilterPropertyListingRequest request);
    }
}
