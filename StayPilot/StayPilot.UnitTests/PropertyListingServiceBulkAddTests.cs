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
        public List<PropertyListing> Saved = new();
        public bool ThrowOnSaveChanges = false;

        // Fails only the first batch, so a test can check the batches after it still save.
        public bool ThrowOnFirstSaveOnly = false;

        public int DiscardPendingChangesCalls = 0;

        private int _saveCalls = 0;
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
            _saveCalls++;

            if (ThrowOnSaveChanges || (ThrowOnFirstSaveOnly && _saveCalls == 1))
            {
                throw new InvalidOperationException("Simulated unique-index violation.");
            }
            return Task.CompletedTask;
        }

        public void DiscardPendingChanges() => DiscardPendingChangesCalls++;

        public Task<List<PropertyListing>> GetComparablePropertyListingAsync(int marketId, PropertyType propertyType, Typology typology, int areaM2, int? distanceToBeachMeters, decimal latitude, decimal longitude, int radiusMeters, int months) => throw new NotImplementedException();
        public Task<List<PropertyListing>> GetAllListingsForFeaturePremiumCalculationAsync() => throw new NotImplementedException();
        public Task<List<MarketAreaStatsListingRow>> GetAllListingsForMarketAreaStatsAsync() => throw new NotImplementedException();
        public Task<List<MarketOverviewListingRow>> GetListingsForMarketOverviewAsync(string? district, string? municipality, string? town, PropertyType? propertyType, Typology? typology) => throw new NotImplementedException();

        public Task<List<PropertyListing>> GetListingsWithHistoryAsync(string? district, string? municipality, string? town) => throw new NotImplementedException();
    }

    file class FakeMarketAreaRepo : IMarketAreaRepository
    {
        private readonly List<MarketArea> _areas;
        public FakeMarketAreaRepo(List<MarketArea> areas) => _areas = areas;
        public Task<List<MarketArea>> GetAllMarketAreasAsync() => Task.FromResult(_areas);
        public Task<MarketArea?> GetMarketAreaByIdAsync(int id) => Task.FromResult(_areas.FirstOrDefault(x => x.Id == id));
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
            Typology = Typology.T2,
            Longitude = -8.10m,
            ListingSnapshot = new ListingSnapshotRequest { Price = price, PricePerM2 = price / 80, Status = ListingStatus.Active }
        };

        // A listing already in the database, with the one snapshot it was saved with.
        // Ids are set because that is what tells a saved row from one we only queued.
        private static PropertyListing ExistingListing(string url, decimal price) => new()
        {
            Id = 1,
            SourceUrl = url,
            MarketAreaId = MarketArea.Id,

            // The real repository Includes this, and the mapper refuses to run without it.
            MarketArea = MarketArea,
            Latitude = 37.07m,
            Longitude = -8.10m,
            ListingSnapshots = new List<ListingSnapshot>
            {
                new() { Id = 1, Price = price, PricePerM2 = price / 80, Status = ListingStatus.Active, SnapshotDateUtc = DateTime.UtcNow.AddDays(-1) }
            }
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
            Assert.True(response.Succeeded);
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

            var error = Assert.Single(response.Errors!);
            Assert.Equal((int)ErrorCode.ListingNotSaved, error.ErrorCode);

            // The url has to survive into the message - it is all the caller has to tell its
            // listings apart now that they are not the keys of a dictionary.
            Assert.Contains("https://example.com/listing-1", error.ErrorMessage);
        }

        [Fact]
        public async Task BulkAdd_OneBatchFails_TheNextBatchStillSaves()
        {
            // Two batches: BatchSize is 100, so 101 listings make the second one.
            var propertyRepo = new FakePropertyListingRepo(new List<PropertyListing>()) { ThrowOnFirstSaveOnly = true };
            var service = new PropertyListingService(propertyRepo, new FakeMarketAreaRepo(new List<MarketArea> { MarketArea }), new FakeBeachMarkerRepo(), new FakeListingSnapshotRepo());

            var items = new List<PropertyListingRequest>();

            for (var i = 0; i < 101; i++)
            {
                items.Add(NewListingRequest($"https://example.com/listing-{i}", 100_000m));
            }

            var response = await service.BulkAddPropertyListingAsync(new BulkAddPropertyListingRequest { Items = items });

            // The first batch is lost and reported; the last listing is in the second batch and lands.
            Assert.Equal(1, response.TotalAdded);
            Assert.Equal(100, response.Errors!.Count);

            // The failed batch has to be thrown away, or it is sent again with the next one.
            Assert.Equal(1, propertyRepo.DiscardPendingChangesCalls);
        }

        [Fact]
        public async Task BulkAdd_BatchFails_UnchangedListingsAreNotReportedAsLost()
        {
            var existing = ExistingListing("https://example.com/listing-1", 100_000m);
            var propertyRepo = new FakePropertyListingRepo(new List<PropertyListing> { existing }) { ThrowOnSaveChanges = true };
            var service = new PropertyListingService(propertyRepo, new FakeMarketAreaRepo(new List<MarketArea> { MarketArea }), new FakeBeachMarkerRepo(), new FakeListingSnapshotRepo());

            // Same price as the one already saved, so this listing writes nothing at all.
            var request = new BulkAddPropertyListingRequest
            {
                Items = new List<PropertyListingRequest> { NewListingRequest("https://example.com/listing-1", 100_000m) }
            };

            var response = await service.BulkAddPropertyListingAsync(request);

            Assert.Equal(1, response.Unchanged);
            Assert.Null(response.Errors);
        }

        [Fact]
        public async Task BulkAdd_ListingWeAlreadyHaveAtANewPrice_AddsASnapshotAndNoSecondListing()
        {
            var existing = ExistingListing("https://example.com/listing-1", 100_000m);

            var propertyRepo = new FakePropertyListingRepo(new List<PropertyListing> { existing });
            var snapshotRepo = new FakeListingSnapshotRepo();
            var service = new PropertyListingService(propertyRepo, new FakeMarketAreaRepo(new List<MarketArea> { MarketArea }), new FakeBeachMarkerRepo(), snapshotRepo);

            var request = new BulkAddPropertyListingRequest
            {
                Items = new List<PropertyListingRequest> { NewListingRequest("https://example.com/listing-1", 90_000m) }
            };

            var response = await service.BulkAddPropertyListingAsync(request);

            // The price history grows; the property itself is not written again.
            Assert.Equal(1, response.SnapShotUpdated);
            Assert.Equal(0, response.TotalAdded);
            Assert.Empty(propertyRepo.Saved);

            var snapshot = Assert.Single(snapshotRepo.Saved);
            Assert.Equal(90_000m, snapshot.Price);
        }

        [Fact]
        public async Task BulkAdd_AddressMatchesNoMarketArea_IsReportedAndNeverSaved()
        {
            var propertyRepo = new FakePropertyListingRepo(new List<PropertyListing>());
            var service = new PropertyListingService(propertyRepo, new FakeMarketAreaRepo(new List<MarketArea> { MarketArea }), new FakeBeachMarkerRepo(), new FakeListingSnapshotRepo());

            var elsewhere = NewListingRequest("https://example.com/listing-1", 100_000m);
            elsewhere.MarketAreaId = null;
            elsewhere.Country = "Spain";
            elsewhere.District = "Madrid";
            elsewhere.Municipality = "Madrid";
            elsewhere.Town = "Madrid";

            var request = new BulkAddPropertyListingRequest { Items = new List<PropertyListingRequest> { elsewhere } };

            var response = await service.BulkAddPropertyListingAsync(request);

            Assert.Empty(propertyRepo.Saved);

            var error = Assert.Single(response.Errors!);
            Assert.Equal((int)ErrorCode.ListingMarketAreaNotFound, error.ErrorCode);

            // Both the listing and the address it could not be placed at.
            Assert.Contains("https://example.com/listing-1", error.ErrorMessage);
            Assert.Contains("Madrid", error.ErrorMessage);
        }

        [Fact]
        public async Task BulkAdd_TypologyMissing_IsReportedAndNeverSaved()
        {
            var propertyRepo = new FakePropertyListingRepo(new List<PropertyListing>());
            var service = new PropertyListingService(propertyRepo, new FakeMarketAreaRepo(new List<MarketArea> { MarketArea }), new FakeBeachMarkerRepo(), new FakeListingSnapshotRepo());

            // Typology 0 - what a caller sends when it has no room count, or one we cannot name.
            // Stored, it leaves the API as a bare number and blanks the screens that read it as text.
            var noTypology = NewListingRequest("https://example.com/listing-1", 100_000m);
            noTypology.Typology = Typology.Unknown;

            var request = new BulkAddPropertyListingRequest { Items = new List<PropertyListingRequest> { noTypology } };

            var response = await service.BulkAddPropertyListingAsync(request);

            Assert.Empty(propertyRepo.Saved);

            var error = Assert.Single(response.Errors!);
            Assert.Equal((int)ErrorCode.ListingTypologyRequired, error.ErrorCode);
            Assert.Contains("https://example.com/listing-1", error.ErrorMessage);
        }

        [Fact]
        public async Task FilterPropertyListing_MinAboveMax_IsReportedAndNeverQueried()
        {
            var propertyRepo = new FakePropertyListingRepo(new List<PropertyListing>());
            var service = new PropertyListingService(propertyRepo, new FakeMarketAreaRepo(new List<MarketArea> { MarketArea }), new FakeBeachMarkerRepo(), new FakeListingSnapshotRepo());

            var request = new FilterPropertyListingRequest { MinPrice = 900_000m, MaxPrice = 100_000m };

            // The fake throws on FilterPropertyAsync, so reaching the database fails this test:
            // an impossible range must be answered without asking for rows that cannot exist.
            var response = await service.FilterPropertyListingAsync(request);

            var error = Assert.Single(response.Errors!);
            Assert.Equal((int)ErrorCode.FilterRangeInverted, error.ErrorCode);
            Assert.Contains("price", error.ErrorMessage);
        }
    }
}
