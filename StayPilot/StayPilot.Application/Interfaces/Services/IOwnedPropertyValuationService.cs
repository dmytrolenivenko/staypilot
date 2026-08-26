
using StayPilot.Application.Contracts.Response;

namespace StayPilot.Application.Interfaces.Services
{
    /// <summary>
    /// Prices the properties the user owns against the listings around them.
    /// Nothing here throws for a request we simply cannot honour: the response comes back
    /// carrying the error instead, and the controller turns that into the HTTP status.
    /// </summary>
    public interface IOwnedPropertyValuationService
    {
        /// <summary>
        /// Work out what one owned property is worth, priced straight off the comparable listings
        /// around it. Comes back carrying OwnedPropertyNotFound when there is no such property,
        /// and Low confidence with no comps when nothing nearby can back a price.
        /// </summary>
        Task<OwnedPropertyAnalysisResponse> EstimateOwnedPropertyValue(int id, int radiusMeters, int months);

        /// <summary>
        /// Prices every owned property, each against the comparable listings around it, and adds
        /// what its place is doing: how keen buyers are there, and where the value is heading over
        /// the next few years.
        ///
        /// Comes back with an empty list when the user simply owns nothing yet.
        /// </summary>
        /// <param name="radiusMeters">How far out comparable adverts still count.</param>
        /// <param name="months">How far back a comparable advert may have last been seen.</param>
        /// <param name="years">How many years the projections run for.</param>
        Task<OwnedPropertyPortfolioResponse> GetPortfolioAsync(int radiusMeters, int months, int years);
    }
}
