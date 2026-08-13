using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Application.Services;
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;

namespace StayPilot.UnitTests
{
    // Hand-written fakes - this project has no Moq dependency (see CalculatorTests / ValuationModelTests).
    file class FakePropertyListingRepo : IPropertyListingRepository
    {
        public List<PropertyListing> Saved = new();
        public bool ThrowOnSaveChanges = false;
        private readonly List<PropertyListing> _existing;

        public FakePropertyListingRepo(List<PropertyListing> existing) => _existing = existing;

        public Task<PropertyListing?> GetPropertyListingByIdAsync(int id) => Task.FromResult<PropertyListing?>(null);

        public Task<List<PropertyListing>?> GetBulkPropertyListingByUrlAsync(List<string> urls) =>
            Task.FromResult<List<PropertyListing>?>(_existing.Where(x => urls.Contains(x.SourceUrl)).ToList());

        public Task<PropertyListing> AddPropertyListingAsync(PropertyListing propertyListing)
        {
            Saved.Add(propertyListing);
            return Task.FromResult(propertyListing);
        }

        public Task<(List<PropertyListing> Items, int TotalRecords)> FilterPropertyAsync(FilterPropertyListingRequest request) => throw new NotImplementedException();

        public Task SaveChangesAsync()
        {
            if (ThrowOnSaveChanges)
            {
                throw new InvalidOperationException("Simulated unique-index violation.");
            }
            return Task.CompletedTask;
        }

        public Task<List<PropertyListing>> GetComparablePropertyListingAsync(int marketId, PropertyType propertyType, Typology typology, int areaM2, decimal? latitude, decimal? longitude, int radiusMeters, int months) => throw new NotImplementedException();
        public Task<List<PropertyListing>> GetAllListingsForFeaturePremiumCalculationAsync() => throw new NotImplementedException();
    }

    file class FakeMarketAreaRepo : IMarketAreaRepository
    {
        private readonly List<MarketArea> _areas;
        public FakeMarketAreaRepo(List<MarketArea> areas) => _areas = areas;
        public Task<List<MarketArea>> GetAllMarketAreasAsync() => Task.FromResult(_areas);
        public Task<(List<MarketArea> Items, int TotalRecords)> GetMarketAreasPageAsync(MarketAreaRequest request) => throw new NotImplementedException();
        public Task<List<string>> GetMarketAreaOptionsAsync(string? distrinct, string? municipality, string? town) => throw new NotImplementedException();
    }

    file class FakeBeachMarkerRepo : IBeachMarkerRepository
    {
        public Task<List<BeachMarker>> GetAllBeachMarkersAsync() => Task.FromResult(new List<BeachMarker>());
    }

    file class FakeListingSnapshotRepo : IListingSnapshotRepository
    {
        public List<ListingSnapshot> Saved = new();
        public Task<ListingSnapshot?> GetListingSnapshotByPropertyIdAsync(int propertyId) => Task.FromResult<ListingSnapshot?>(null);
        public Task<ListingSnapshot> AddListingSnapshotAsync(ListingSnapshot listingSnapshot) { Saved.Add(listingSnapshot); return Task.FromResult(listingSnapshot); }
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    public class PropertyListingServiceBulkAddTests
    {
        private static readonly MarketArea MarketArea = new() { Id = 1, Country = "Portugal", District = "Faro", Municipality = "Loule", Town = "Quarteira" };

        private static PropertyListingRequest NewListingRequest(string url, decimal price) => new()
        {
            SourceUrl = url,
            MarketAreaId = MarketArea.Id,
            Latitude = 37.07m,
            Longitude = -8.10m,
            ListingSnapshot = new ListingSnapshotRequest { Price = price, PricePerM2 = price / 80, Status = ListingStatus.Active }
        };

        [Fact]
        public async Task BulkAdd_TwoBrandNewListings_CountsThemAsAdded()
        {
            var propertyRepo = new FakePropertyListingRepo(new List<PropertyListing>());
            var service = new PropertyListingService(propertyRepo, new FakeMarketAreaRepo(new List<MarketArea> { MarketArea }), new FakeBeachMarkerRepo(), new FakeListingSnapshotRepo());

            var request = new BulkAddPropertyListingRequest
            {
                Items = new List<PropertyListingRequest>
                {
                    NewListingRequest("https://example.com/listing-1", 100_000m),
                    NewListingRequest("https://example.com/listing-2", 200_000m),
                }
            };

            var response = await service.BulkAddPropertyListingAsync(request);

            Assert.Equal(2, propertyRepo.Saved.Count);
            Assert.Equal(2, response.TotalAdded);
            Assert.Equal(0, response.Unchanged);
            Assert.Empty(response.FailedListings);
        }

        [Fact]
        public async Task BulkAdd_SameNewUrlTwiceInOneRequest_UpdatesTheSecondInsteadOfDoubleInserting()
        {
            var propertyRepo = new FakePropertyListingRepo(new List<PropertyListing>());
            var service = new PropertyListingService(propertyRepo, new FakeMarketAreaRepo(new List<MarketArea> { MarketArea }), new FakeBeachMarkerRepo(), new FakeListingSnapshotRepo());

            var request = new BulkAddPropertyListingRequest
            {
                Items = new List<PropertyListingRequest>
                {
                    NewListingRequest("https://example.com/listing-1", 100_000m),
                    NewListingRequest("https://example.com/listing-1", 150_000m), // same URL, price changed - captured twice in one upload
                }
            };

            var response = await service.BulkAddPropertyListingAsync(request);

            // Only one PropertyListing row for that URL - the second occurrence must not try to insert a duplicate.
            Assert.Single(propertyRepo.Saved);
            Assert.Equal(1, response.TotalAdded);
            Assert.Equal(1, response.SnapShotUpdated);
        }

        [Fact]
        public async Task BulkAdd_BatchSaveFails_ReportsTheWholeBatchAsFailedInsteadOfThrowing()
        {
            var propertyRepo = new FakePropertyListingRepo(new List<PropertyListing>()) { ThrowOnSaveChanges = true };
            var service = new PropertyListingService(propertyRepo, new FakeMarketAreaRepo(new List<MarketArea> { MarketArea }), new FakeBeachMarkerRepo(), new FakeListingSnapshotRepo());

            var request = new BulkAddPropertyListingRequest
            {
                Items = new List<PropertyListingRequest> { NewListingRequest("https://example.com/listing-1", 100_000m) }
            };

            var response = await service.BulkAddPropertyListingAsync(request);

            Assert.Equal(0, response.TotalAdded);
            Assert.Equal(0, response.Unchanged);
            Assert.Single(response.FailedListings);
            Assert.Contains("https://example.com/listing-1", response.FailedListings.Keys);
        }
    }
}
