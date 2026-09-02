
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

        /// <summary>
        /// Reads back the last priced result for every owned property - a plain table read, no
        /// model fit and no comp search. A property never valued yet comes back as a placeholder
        /// row (ValuatedAtUtc null) rather than being left out, so the list still lines up with
        /// GetAllOwnedProperty.
        ///
        /// Empty when the user simply owns nothing yet. Use <see cref="RevalueOwnedPropertiesAsync"/>
        /// or <see cref="RevalueOwnedPropertyAsync"/> to price what is missing.
        /// </summary>
        Task<OwnedPropertyPortfolioResponse> GetCachedPortfolioAsync();

        /// <summary>
        /// Prices every owned property in one pass, and adds what its place is doing around it:
        /// how keen buyers are there, and where the value is heading over the next few years.
        /// Writes the result into the cache GetCachedPortfolioAsync reads, overwriting whatever
        /// was there before - this is the "Re-price" action, not a read.
        ///
        /// One fit rather than one per property because the valuation model is fitted over the
        /// whole listing table - fitting once and pricing ten is the work of pricing one.
        ///
        /// Comes back carrying NotEnoughListingsToFitModel when there is too little market data
        /// to price anything - nothing stored is changed in that case.
        /// </summary>
        /// <param name="radiusMeters">How far out comparable adverts still count.</param>
        /// <param name="months">How far back a comparable advert may have last been seen.</param>
        /// <param name="years">How many years the projections run for.</param>
        Task<OwnedPropertyPortfolioResponse> RevalueOwnedPropertiesAsync(int radiusMeters, int months, int years);

        /// <summary>
        /// Prices one owned property and overwrites its stored valuation, the same way
        /// <see cref="RevalueOwnedPropertiesAsync"/> does for the whole portfolio.
        /// Comes back carrying OwnedPropertyNotFound, or NotEnoughListingsToFitModel when there
        /// is too little market data to price anything - nothing stored changes in either case.
        /// </summary>
        /// <param name="id">The owned property to revalue.</param>
        /// <param name="radiusMeters">How far out comparable adverts still count.</param>
        /// <param name="months">How far back a comparable advert may have last been seen.</param>
        /// <param name="years">How many years the projection runs for.</param>
        Task<OwnedPropertyValuationResponse> RevalueOwnedPropertyAsync(int id, int radiusMeters, int months, int years);
    }
}
