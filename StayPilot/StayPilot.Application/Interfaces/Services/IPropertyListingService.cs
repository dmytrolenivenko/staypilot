using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;

namespace StayPilot.Application.Interfaces.Services
{
    /// <summary>
    /// Handles property listings for the caller.
    /// </summary>
    public interface IPropertyListingService
    {
        /// <summary>
        /// Get one property by its Id.
        /// Comes back carrying PropertyListingNotFound when there is no such property.
        /// </summary>
        Task<PropertyListingResponse> GetPropertyListingByIdAsync(int propertyId);

        /// <summary>
        /// Save many listings in one call.
        /// Every listing is checked first, and only the ones that pass are saved. The rest come
        /// back in Errors with the reason, and never reach the database.
        /// </summary>
        Task<BulkAddPropertyListingResponse> BulkAddPropertyListingAsync(BulkAddPropertyListingRequest request);

        /// <summary>
        /// Search properties with filters, one page at a time.
        /// Returns the page of items and the total number of matches.
        /// </summary>
        Task<FilterPropertyListingResponse> FilterPropertyListingAsync(FilterPropertyListingRequest request);
    }
}
