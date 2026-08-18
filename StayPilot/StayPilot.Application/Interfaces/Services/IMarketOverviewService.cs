using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;

namespace StayPilot.Application.Interfaces.Services
{
    /// <summary>
    /// Answers "what is this bit of the market asking?" for one place and one kind of property.
    /// </summary>
    public interface IMarketOverviewService
    {
        /// <summary>
        /// The overview for the slice in the request: the listing count, price, price for each
        /// square meter and floor area each summarised four ways, the price distribution, and a
        /// row per room layout found there.
        ///
        /// Worked out from the listings on every call rather than read from the stats table, so
        /// any combination of place, property type and layout can be asked for. A slice with no
        /// listings comes back with a count of zero and no numbers - that is an answer, not an
        /// error, so it carries no error code.
        /// </summary>
        Task<MarketOverviewResponse> GetMarketOverviewAsync(MarketOverviewRequest request);
    }
}
