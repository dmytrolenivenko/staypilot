using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response.Base;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Application.Interfaces.Services;
using StayPilot.Application.Services;
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;

namespace StayPilot.UnitTests
{
    // Hand-written fakes - this project has no Moq dependency.

    // Answers a fixed user id, the way CurrentUser answers the one on the request.
    file class FakeCurrentUser : ICurrentUser
    {
        private readonly int _userId;

        public FakeCurrentUser(int userId) => _userId = userId;

        public Task<int> GetCurrentUserIdAsync() => Task.FromResult(_userId);
    }

    // Applies the same owner filter the real repository applies, because that filter is half of
    // what is under test here - a fake that ignored ownerUserId would let every one of these
    // tests pass against a service that had dropped the argument entirely.
    file class FakeOwnedPropertyRepo : IOwnedPropertyRepository
    {
        private readonly List<OwnedProperty> _rows;

        public FakeOwnedPropertyRepo(params OwnedProperty[] rows) => _rows = rows.ToList();

        public IReadOnlyList<OwnedProperty> Rows => _rows;

        public int SaveCount { get; private set; }

        public Task<OwnedProperty?> GetOwnedPropertyAsync(int id, int ownerUserId) =>
            Task.FromResult(_rows.FirstOrDefault(x => x.Id == id && x.OwnerUserId == ownerUserId));

        public Task<List<OwnedProperty>> GetAllOwnedPropertyAsync(int ownerUserId) =>
            Task.FromResult(_rows.Where(x => x.OwnerUserId == ownerUserId).ToList());

        public Task<string?> DeleteOwnedPropertyAsync(int id, int ownerUserId)
        {
            var row = _rows.FirstOrDefault(x => x.Id == id && x.OwnerUserId == ownerUserId);

            if (row is null)
            {
                return Task.FromResult<string?>(null);
            }

            _rows.Remove(row);

            return Task.FromResult<string?>(row.Name);
        }

        public Task<OwnedProperty> CreateOwnedPropertyAsync(OwnedProperty entity)
        {
            entity.Id = _rows.Count == 0 ? 1 : _rows.Max(x => x.Id) + 1;
            _rows.Add(entity);

            return Task.FromResult(entity);
        }

        public Task SaveChangesAsync()
        {
            SaveCount++;

            return Task.CompletedTask;
        }

        public Task<Dictionary<int, OwnedPropertyValuation>> GetAllValuationsAsync() =>
            Task.FromResult(new Dictionary<int, OwnedPropertyValuation>());

        public Task<OwnedProperty?> UpdateOwnedPropertyAsync(OwnedProperty entity) => throw new NotImplementedException();
        public Task UpsertValuationAsync(OwnedPropertyValuation valuation) => throw new NotImplementedException();
    }

    // One market area, enough for GetMarketId to place the test address.
    file class FakeMarketAreaRepo : IMarketAreaRepository
    {
        public static readonly MarketArea Quarteira = new()
        {
            Id = 42,
            Country = "Portugal",
            District = "Faro",
            Municipality = "Loule",
            Town = "Quarteira",
        };

        public Task<List<MarketArea>> GetAllMarketAreasAsync() => Task.FromResult(new List<MarketArea> { Quarteira });

        public Task<(List<MarketArea> Items, int TotalRecords)> GetMarketAreasPageAsync(MarketAreaRequest request) => throw new NotImplementedException();
        public Task<List<string>> GetMarketAreaOptionsAsync(string? district, string? municipality, string? town) => throw new NotImplementedException();
    }

    // No beaches: the property then just gets null beach fields, which is a normal case.
    file class FakeBeachRepo : IBeachMarkerRepository
    {
        public Task<List<BeachMarker>> GetAllBeachMarkersAsync() => Task.FromResult(new List<BeachMarker>());
    }

    file class FakePremiumFeatureRepo : IPremiumFeatureRepository
    {
        public Task<List<PremiumFeature>> GetAllPremiumFeaturesAsync() => Task.FromResult(new List<PremiumFeature>());

        public Task<PremiumFeature> AddPremiumFeatureAsync(PremiumFeature premiumFeature) => throw new NotImplementedException();
        public void RemovePremiumFeatures(IEnumerable<PremiumFeature> premiumFeatures) => throw new NotImplementedException();
        public Task SaveChangesAsync() => throw new NotImplementedException();
    }

    // The CRUD paths never reach the listing or growth repositories - only valuation does.
    file class UnusedListingRepoForOwned : IPropertyListingRepository
    {
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
        public Task<List<PropertyListing>> GetActiveListingsAsync() => throw new NotImplementedException();
    }

    file class UnusedGrowthRepo : IHousePriceGrowthRepository
    {
        public Task<HousePriceGrowth?> GetForDistrictAsync(string district) => throw new NotImplementedException();
        public Task<List<HousePriceGrowth>> GetAllAsync() => throw new NotImplementedException();
    }

    /// <summary>
    /// Every OwnedProperty read and write is scoped to whoever is signed in. Nothing tested that
    /// before, even though it is the whole of the tenant boundary: the id is a bare sequential
    /// int, so if a method ever stops passing the caller's id down, one tenant can read, edit and
    /// delete another's properties just by guessing the number.
    /// </summary>
    public class OwnedPropertyServiceTenantIsolationTests
    {
        private const int Alice = 1;
        private const int Bob = 2;

        private static OwnedProperty PropertyOwnedBy(int ownerUserId, int id, string name) => new()
        {
            Id = id,
            OwnerUserId = ownerUserId,
            Name = name,
            MarketAreaId = FakeMarketAreaRepo.Quarteira.Id,
            PropertyType = PropertyType.Apartment,
            Typology = Typology.T2,
            AreaM2 = 73,
        };

        private static OwnedPropertyRequest QuarteiraRequest(string name) => new()
        {
            Name = name,
            Country = "Portugal",
            District = "Faro",
            Municipality = "Loule",
            Town = "Quarteira",
            PropertyType = PropertyType.Apartment,
            Typology = Typology.T2,
            AreaM2 = 73,
        };

        // Takes the interface, not the fake: a file-local type cannot appear in the signature of
        // a member on a public class.
        private static OwnedPropertyService ServiceFor(int callerUserId, IOwnedPropertyRepository repo) =>
            new(
                repo,
                new FakeMarketAreaRepo(),
                new FakeBeachRepo(),
                new UnusedListingRepoForOwned(),
                new FakePremiumFeatureRepo(),
                new UnusedGrowthRepo(),
                new FakeCurrentUser(callerUserId));

        [Fact]
        public async Task GetOwnedPropertyAsync_PropertyBelongsToAnotherTenant_ReportsNotFound()
        {
            var repo = new FakeOwnedPropertyRepo(PropertyOwnedBy(Alice, id: 7, "Alice's flat"));

            var response = await ServiceFor(Bob, repo).GetOwnedPropertyAsync(7);

            Assert.False(response.Succeeded);
            Assert.Contains(response.Errors!, x => x.ErrorCode == (int)ErrorCode.OwnedPropertyNotFound);
        }

        [Fact]
        public async Task GetOwnedPropertyAsync_OwnProperty_ReturnsIt()
        {
            var repo = new FakeOwnedPropertyRepo(PropertyOwnedBy(Alice, id: 7, "Alice's flat"));

            var response = await ServiceFor(Alice, repo).GetOwnedPropertyAsync(7);

            Assert.True(response.Succeeded);
            Assert.Equal("Alice's flat", response.Name);
        }

        [Fact]
        public async Task DeleteOwnedPropertyAsync_PropertyBelongsToAnotherTenant_DeletesNothing()
        {
            var repo = new FakeOwnedPropertyRepo(PropertyOwnedBy(Alice, id: 7, "Alice's flat"));

            var response = await ServiceFor(Bob, repo).DeleteOwnedPropertyAsync(7);

            Assert.False(response.Succeeded);
            Assert.Contains(response.Errors!, x => x.ErrorCode == (int)ErrorCode.OwnedPropertyNotFound);

            // The row is still there, and nothing was even staged for saving.
            Assert.Single(repo.Rows);
            Assert.Equal(0, repo.SaveCount);
        }

        [Fact]
        public async Task UpdateOwnedPropertyAsync_PropertyBelongsToAnotherTenant_ChangesNothing()
        {
            var repo = new FakeOwnedPropertyRepo(PropertyOwnedBy(Alice, id: 7, "Alice's flat"));

            var response = await ServiceFor(Bob, repo).UpdateOwnedPropertyAsync(7, QuarteiraRequest("Bob took it"));

            Assert.False(response.Succeeded);
            Assert.Contains(response.Errors!, x => x.ErrorCode == (int)ErrorCode.OwnedPropertyNotFound);
            Assert.Equal("Alice's flat", repo.Rows.Single().Name);
            Assert.Equal(0, repo.SaveCount);
        }

        [Fact]
        public async Task AddOwnedPropertyAsync_StampsTheSignedInCallerAsOwner()
        {
            var repo = new FakeOwnedPropertyRepo();

            var response = await ServiceFor(Bob, repo).AddOwnedPropertyAsync(QuarteiraRequest("Bob's flat"));

            Assert.True(response.Succeeded);
            Assert.Equal(Bob, repo.Rows.Single().OwnerUserId);
        }

        [Fact]
        public async Task GetAllOwnedPropertiesAsync_ReturnsOnlyTheCallersRows()
        {
            var repo = new FakeOwnedPropertyRepo(
                PropertyOwnedBy(Alice, id: 1, "Alice one"),
                PropertyOwnedBy(Alice, id: 2, "Alice two"),
                PropertyOwnedBy(Bob, id: 3, "Bob one"));

            var response = await ServiceFor(Bob, repo).GetAllOwnedPropertiesAsync();

            Assert.Equal(new[] { "Bob one" }, response.Items.Select(x => x.Name));
        }
    }
}
