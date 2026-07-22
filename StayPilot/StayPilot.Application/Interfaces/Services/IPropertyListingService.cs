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
        /// Save a new property. If it already exists (same URL), the existing one is returned.
        /// Also sets its market area and its closest beach.
        /// </summary>
        Task<PropertyListingResponse> AddPropertyListingAsync(PropertyListingRequest propertyListing);

        /// <summary>
        /// Search properties with filters, one page at a time.
        /// Returns the page of items and the total number of matches.
        /// </summary>
        Task<ListPropertyListingResponse> FilterPropertyAsync(ListPropertyListingRequest request);
    }
}
