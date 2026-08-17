
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Contracts.Request;

namespace StayPilot.Application.Interfaces.Services
{
    /// <summary>
    /// Handles the properties the user owns, and what they are worth today.
    /// Nothing here throws for a request we simply cannot honour: the response comes back
    /// carrying the error instead, and the controller turns that into the HTTP status.
    /// </summary>
    public interface IOwnedPropertyService
    {
        /// <summary>
        /// Get one owned property by its Id.
        /// Comes back carrying OwnedPropertyNotFound when there is no such property.
        /// </summary>
        Task<OwnedPropertyResponse> GetOwnedPropertyAsync(int id);

        /// <summary>
        /// Save a new owned property, placing it in the market area its address points at.
        /// Comes back carrying MarketAreaNotFound when no area matches, and saves nothing.
        /// </summary>
        Task<OwnedPropertyResponse> AddOwnedPropertyAsync(OwnedPropertyRequest request);

        /// <summary>
        /// Delete one owned property.
        /// Comes back carrying OwnedPropertyNotFound when there is no such property.
        /// </summary>
        Task<DeleteOwnedPropertyResponse> DeleteOwnedPropertyAsync(int id);

        /// <summary>
        /// Change one owned property. Only the fields the caller sent are touched.
        /// Comes back carrying OwnedPropertyNotFound or MarketAreaNotFound, and changes nothing
        /// at all in either case.
        /// </summary>
        Task<OwnedPropertyResponse> UpdateOwnedPropertyAsync(int id, OwnedPropertyRequest request);

        /// <summary>
        /// Work out what one owned property is worth, against the listings around it.
        /// Comes back carrying OwnedPropertyNotFound, or NotEnoughListingsToFitModel when there
        /// is too little market data to price anything.
        /// </summary>
        Task<OwnedPropertyAnalysisResponse> EstimateOwnedPropertyValue(int id, int radiusMeters, int months);

        /// <summary>
        /// Every owned property of the user. Empty when there are none.
        /// </summary>
        Task<OwnedPropertyListResponse> GetAllOwnedPropertiesAsync();
    }
}
