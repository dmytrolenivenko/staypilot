using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Application.Interfaces.Services;
using StayPilot.Application.ReadModels;
using StayPilot.Application.Services;
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;

namespace StayPilot.UnitTests
{
    // Counts calls - each real one is a paid AI request.
    internal sealed class CountingNarrativeClient : IInvestmentNarrativeClient
    {
        public int Calls { get; private set; }

        public Task<string?> GenerateNarrativeAsync(InvestmentAnalysisResponse result, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult<string?>("A thesis.");
        }
    }

    file class OneListingRepo(PropertyListing listing) : IPropertyListingRepository
    {
        public Task<PropertyListing?> GetPropertyListingByIdAsync(int id) => Task.FromResult<PropertyListing?>(listing);

        public Task<List<PropertyListing>?> GetBulkPropertyListingByUrlAsync(List<string> urls) => throw new NotImplementedException();
        public Task<PropertyListing> AddPropertyListingAsync(PropertyListing propertyListing) => throw new NotImplementedException();
        public Task<(List<PropertyListing> Items, int TotalRecords)> FilterPropertyAsync(FilterPropertyListingRequest request) => throw new NotImplementedException();
        public Task SaveChangesAsync() => throw new NotImplementedException();
        public void DiscardPendingChanges() => throw new NotImplementedException();
        public Task<List<PropertyListing>> GetComparablePropertyListingAsync(int marketId, PropertyType propertyType, Typology typology, int areaM2, int? distanceToBeachMeters, decimal? latitude, decimal? longitude, int radiusMeters, int months) => throw new NotImplementedException();
        public Task<List<PropertyListing>> GetAllListingsForFeaturePremiumCalculationAsync() => throw new NotImplementedException();
        public Task<List<OverviewListing>> GetListingsForMarketOverviewAsync(string? district, string? municipality, string? town, PropertyType? propertyType, Typology? typology) => throw new NotImplementedException();
        public Task<List<TopDealCandidate>> GetActiveListingsForTopDealsAsync(string? district, string? municipality, string? town, string? zone, PropertyCondition? condition) => throw new NotImplementedException();
        public Task<List<PropertyListing>> GetPropertyListingsByIdsAsync(IReadOnlyCollection<int> ids) => throw new NotImplementedException();
        public Task<List<PropertyListing>> GetActiveListingsAsync() => throw new NotImplementedException();
        public Task<List<PropertyListing>> GetListingsWithHistoryAsync(string? district, string? municipality, string? town) => throw new NotImplementedException();
    }

    file class OneTownStatsRepo(MarketAreaStats stats) : IMarketAreaStatsRepository
    {
        public Task<List<MarketAreaStats>> GetWithTypologiesAsync(AreaLevel level, int minListings, string? district = null, string? municipality = null) =>
            Task.FromResult(new List<MarketAreaStats> { stats });

        public Task<List<MarketAreaStats>> GetAllMarketAreaStatsAsync() => throw new NotImplementedException();
        public Task<List<MarketAreaStats>> GetLeaderboardAsync(AreaLevel level, int minListings, string? district = null, string? municipality = null) => throw new NotImplementedException();
        public Task AddMarketAreaStatsAsync(IEnumerable<MarketAreaStats> stats) => throw new NotImplementedException();
        public void RemoveMarketAreaStats(IEnumerable<MarketAreaStats> stats) => throw new NotImplementedException();
        public Task SaveChangesAsync() => throw new NotImplementedException();
    }

    file class NoIne : IIneRepository
    {
        public Task<ConstructionIndex?> GetConstructionIndexAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<ConstructionIndex?>(null);
    }

    /// <summary>
    /// The listing analysis is public, but the AI narrative costs money per call - so only a
    /// signed-in caller gets one. Anonymous visitors still get every number.
    /// </summary>
    public class InvestmentAnalysisNarrativeGateTests
    {
        private static (InvestmentAnalysisService Service, CountingNarrativeClient Narrative) Build()
        {
            var area = new MarketArea { District = "Faro", Municipality = "Albufeira", Town = "Guia" };

            var listing = new PropertyListing
            {
                Id = 1,
                MarketArea = area,
                Typology = Typology.T2,
                AreaM2 = 90,
                Condition = PropertyCondition.Used,
                ListingSnapshots = { new ListingSnapshot { Price = 250_000m, SnapshotDateUtc = DateTime.UtcNow } }
            };

            var stats = new MarketAreaStats
            {
                Level = AreaLevel.Town,
                District = "Faro",
                Municipality = "Albufeira",
                Town = "Guia",
                TypologyStats =
                {
                    new MarketAreaTypologyStats { Typology = Typology.T2, ListingCount = 40, MedianAreaM2 = 90m, MedianPricePerM2 = 3_500m }
                }
            };

            var narrative = new CountingNarrativeClient();

            var service = new InvestmentAnalysisService(
                new OneListingRepo(listing),
                ownedPropertyRepo: null!,
                marketAreaRepo: null!,
                new OneTownStatsRepo(stats),
                new BuildCostService(new NoIne()),
                narrative,
                currentUser: null!);

            return (service, narrative);
        }

        [Fact]
        public async Task Anonymous_caller_gets_the_numbers_but_no_AI_call()
        {
            var (service, narrative) = Build();

            var result = await service.AnalyzeAsync(1, includeNarrative: false);

            Assert.Equal(0, narrative.Calls);
            Assert.Null(result.Narrative);
            Assert.True(result.NarrativeRequiresSignIn);
            Assert.True(result.EstimatedResaleValue > 0);
        }

        [Fact]
        public async Task Signed_in_caller_gets_the_narrative()
        {
            var (service, narrative) = Build();

            var result = await service.AnalyzeAsync(1, includeNarrative: true);

            Assert.Equal(1, narrative.Calls);
            Assert.Equal("A thesis.", result.Narrative);
            Assert.False(result.NarrativeRequiresSignIn);
        }
    }
}
