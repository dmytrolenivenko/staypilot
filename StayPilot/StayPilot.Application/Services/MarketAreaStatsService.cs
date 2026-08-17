using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Contracts.Response.Base;
using StayPilot.Application.Helpers.Calculators;
using StayPilot.Application.Helpers.Mappers;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Application.Interfaces.Services;

namespace StayPilot.Application.Services
{
    /// <inheritdoc/>
    public class MarketAreaStatsService : IMarketAreaStatsService
    {
        private readonly IMarketAreaStatsRepository _marketAreaStatsRepo;
        private readonly IPropertyListingRepository _propertyListingRepo;

        public MarketAreaStatsService(IMarketAreaStatsRepository marketAreaStatsRepo, IPropertyListingRepository propertyListingRepo)
        {
            _marketAreaStatsRepo = marketAreaStatsRepo;
            _propertyListingRepo = propertyListingRepo;
        }

        /// <inheritdoc/>
        public async Task<RecalculateMarketAreaStatsResponse> RecalculateMarketAreaStatsAsync()
        {
            var response = new RecalculateMarketAreaStatsResponse();

            // Same load the feature premiums use: every listing, with its market area and its
            // newest price. Reused rather than copied - the shape needed is identical.
            var allListings = await _propertyListingRepo.GetAllListingsForFeaturePremiumCalculationAsync();

            var rows = MarketAreaStatsCalculator.Calculate(allListings);

            if (rows.Count == 0)
            {
                // Nothing could be placed or priced. Say so instead of wiping the table and
                // leaving the screens empty with no explanation.
                response.AddError(ErrorCode.NotEnoughListingsForStats, allListings.Count.ToString());

                return response;
            }

            // Overwrite, not append: exactly one current set of rows.
            var previous = await _marketAreaStatsRepo.GetAllMarketAreaStatsAsync();
            _marketAreaStatsRepo.RemoveMarketAreaStats(previous);

            await _marketAreaStatsRepo.AddMarketAreaStatsAsync(rows);
            await _marketAreaStatsRepo.SaveChangesAsync();

            response.ListingsUsed = rows
                .Where(x => x.Level == Domain.Enums.AreaLevel.District)
                .Sum(x => x.ListingCount);

            response.RowsWritten = rows.Count;
            response.CalculatedAtUtc = rows[0].CalculatedAtUtc;

            return response;
        }

        /// <inheritdoc/>
        public async Task<MarketAreaLeaderboardResponse> GetLeaderboardAsync(MarketAreaLeaderboardRequest request)
        {
            var rows = await _marketAreaStatsRepo.GetLeaderboardAsync(request.Level, request.MinListings);

            return new MarketAreaLeaderboardResponse
            {
                Items = rows.Select(Converter.MapToResponse).ToList(),

                // Every row of one run carries the same stamp, so the first one speaks for all.
                CalculatedAtUtc = rows.Count == 0 ? null : rows[0].CalculatedAtUtc
            };
        }
    }
}
