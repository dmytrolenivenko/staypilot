using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response.Base;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Application.Services;
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;

namespace StayPilot.UnitTests
{
    // Hand-written fakes - this project has no Moq dependency (see CalculatorTests).
    file class FakePropertyListingRepo : IPropertyListingRepository
    {
        private readonly List<PropertyListing> _active;
        public FakePropertyListingRepo(List<PropertyListing> active) => _active = active;

        public Task<List<PropertyListing>> GetActiveListingsAsync() => Task.FromResult(_active);

        public Task<PropertyListing?> GetPropertyListingByIdAsync(int id) => throw new NotImplementedException();
        public Task<List<PropertyListing>?> GetBulkPropertyListingByUrlAsync(List<string> urls) => throw new NotImplementedException();
        public Task<PropertyListing> AddPropertyListingAsync(PropertyListing propertyListing) => throw new NotImplementedException();
        public Task<(List<PropertyListing> Items, int TotalRecords)> FilterPropertyAsync(FilterPropertyListingRequest request) => throw new NotImplementedException();
        public Task SaveChangesAsync() => throw new NotImplementedException();
        public void DiscardPendingChanges() => throw new NotImplementedException();
        public Task<List<PropertyListing>> GetComparablePropertyListingAsync(int marketId, PropertyType propertyType, Typology typology, int areaM2, int? distanceToBeachMeters, decimal? latitude, decimal? longitude, int radiusMeters, int months) => throw new NotImplementedException();
        public Task<List<PropertyListing>> GetAllListingsForFeaturePremiumCalculationAsync() => throw new NotImplementedException();
        public Task<List<PropertyListing>> GetListingsForMarketOverviewAsync(string? district, string? municipality, string? town, PropertyType? propertyType, Typology? typology) => throw new NotImplementedException();
        public Task<List<PropertyListing>> GetActiveListingsForTopDealsAsync(string? district, string? municipality, string? town, string? zone, PropertyCondition? condition) => throw new NotImplementedException();
        public Task<List<PropertyListing>> GetListingsWithHistoryAsync(string? district, string? municipality, string? town) => throw new NotImplementedException();
    }

    file class FakeListingSnapshotRepo : IListingSnapshotRepository
    {
        public List<ListingSnapshot> Saved = new();
        public int SaveChangesCalls = 0;
        public Task<ListingSnapshot?> GetListingSnapshotByPropertyIdAsync(int propertyId) => throw new NotImplementedException();
        public Task<ListingSnapshot> AddListingSnapshotAsync(ListingSnapshot listingSnapshot) { Saved.Add(listingSnapshot); return Task.FromResult(listingSnapshot); }
        public Task SaveChangesAsync() { SaveChangesCalls++; return Task.CompletedTask; }
    }

    public class ListingSnapshotServiceTests
    {
        private static PropertyListing ActiveListing(int id, string url, decimal lastPrice)
        {
            var listing = new PropertyListing { Id = id, SourceUrl = url };
            listing.ListingSnapshots.Add(new ListingSnapshot
            {
                PropertyListingId = id,
                Price = lastPrice,
                PricePerM2 = lastPrice / 80,
                Status = ListingStatus.Active,
                SnapshotDateUtc = DateTime.UtcNow.AddDays(-1)
            });
            return listing;
        }

        [Fact]
        public async Task ReconcileActiveListingsAsync_EmptyActiveUrls_ReturnsErrorAndSavesNothing()
        {
            var snapshotRepo = new FakeListingSnapshotRepo();
            var service = new ListingSnapshotService(snapshotRepo, new FakePropertyListingRepo(new List<PropertyListing>
            {
                ActiveListing(1, "https://example.com/1", 100000)
            }));

            var response = await service.ReconcileActiveListingsAsync(new ReconcileActiveListingsRequest { ActiveUrls = new List<string>() });

            Assert.False(response.Succeeded);
            Assert.Contains(response.Errors!, e => e.ErrorCode == (int)ErrorCode.ReconcileActiveUrlsRequired);
            Assert.Empty(snapshotRepo.Saved);
            Assert.Equal(0, snapshotRepo.SaveChangesCalls);
        }

        [Fact]
        public async Task ReconcileActiveListingsAsync_ListingMissingFromActiveUrls_GetsMarkedSoldWithLastPrice()
        {
            var stillLive = ActiveListing(1, "https://example.com/still-live", 150000);
            var goneMissing = ActiveListing(2, "https://example.com/gone", 200000);

            var snapshotRepo = new FakeListingSnapshotRepo();
            var service = new ListingSnapshotService(snapshotRepo, new FakePropertyListingRepo(new List<PropertyListing> { stillLive, goneMissing }));

            var response = await service.ReconcileActiveListingsAsync(new ReconcileActiveListingsRequest
            {
                ActiveUrls = new List<string> { "https://example.com/still-live" }
            });

            Assert.True(response.Succeeded);
            Assert.Equal(2, response.ActiveListingsChecked);
            Assert.Equal(1, response.MarkedSoldCount);
            Assert.Equal(new[] { "https://example.com/gone" }, response.MarkedSoldUrls);

            var soldSnapshot = Assert.Single(snapshotRepo.Saved);
            Assert.Equal(goneMissing.Id, soldSnapshot.PropertyListingId);
            Assert.Equal(ListingStatus.Sold, soldSnapshot.Status);
            Assert.Equal(200000, soldSnapshot.Price); // carried forward from the last snapshot, not a new price
            Assert.Equal(1, snapshotRepo.SaveChangesCalls);
        }

        [Fact]
        public async Task ReconcileActiveListingsAsync_UrlMatchIsCaseInsensitive()
        {
            var listing = ActiveListing(1, "https://example.com/Listing", 100000);

            var snapshotRepo = new FakeListingSnapshotRepo();
            var service = new ListingSnapshotService(snapshotRepo, new FakePropertyListingRepo(new List<PropertyListing> { listing }));

            var response = await service.ReconcileActiveListingsAsync(new ReconcileActiveListingsRequest
            {
                ActiveUrls = new List<string> { "https://example.com/listing" } // different case, same URL
            });

            Assert.Equal(0, response.MarkedSoldCount);
            Assert.Empty(snapshotRepo.Saved);
        }
    }
}
