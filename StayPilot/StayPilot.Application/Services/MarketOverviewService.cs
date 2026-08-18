using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Helpers.Calculators;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Application.Interfaces.Services;
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Services
{
    /// <inheritdoc/>
    public class MarketOverviewService : IMarketOverviewService
    {
        private readonly IPropertyListingRepository _propertyListingRepo;

        public MarketOverviewService(IPropertyListingRepository propertyListingRepo)
        {
            _propertyListingRepo = propertyListingRepo;
        }

        /// <inheritdoc/>
        public async Task<MarketOverviewResponse> GetMarketOverviewAsync(MarketOverviewRequest request)
        {
            var listings = await _propertyListingRepo.GetListingsForMarketOverviewAsync(
                request.District,
                request.Municipality,
                request.Town,
                request.PropertyType,
                request.Typology);

            var response = MarketOverviewCalculator.Calculate(listings, request.BucketCount, BreakdownLevel(request));

            response.PlaceName = PlaceName(request);

            return response;
        }

        /// <summary>
        /// Which grain to break the slice into: always one step finer than what was asked for.
        ///
        /// Null once the slice is a single freguesia, because there is nothing finer we hold. The
        /// screen then shows the summary alone, which at that grain is genuinely the answer.
        /// </summary>
        private static AreaLevel? BreakdownLevel(MarketOverviewRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.Town))
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(request.Municipality))
            {
                return AreaLevel.Town;
            }

            return string.IsNullOrWhiteSpace(request.District) ? AreaLevel.District : AreaLevel.Municipality;
        }

        /// <summary>
        /// The slice named the way the rest of the screens name a place: the narrowest part the
        /// caller picked, with the part above it in brackets - "Guia (Albufeira)".
        /// </summary>
        private static string PlaceName(MarketOverviewRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.Town))
            {
                return WithParent(request.Town, request.Municipality);
            }

            if (!string.IsNullOrWhiteSpace(request.Municipality))
            {
                return WithParent(request.Municipality, request.District);
            }

            if (!string.IsNullOrWhiteSpace(request.District))
            {
                return request.District.Trim();
            }

            // Nothing was narrowed, so the numbers cover everything we hold. Said plainly,
            // because a median across every district at once describes no market you can buy in.
            return "All areas";
        }

        private static string WithParent(string place, string? parent)
        {
            return string.IsNullOrWhiteSpace(parent)
                ? place.Trim()
                : $"{place.Trim()} ({parent.Trim()})";
        }
    }
}
