using Microsoft.EntityFrameworkCore;
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;
using StayPilot.Infrastructure.Persistence;
using StayPilot.Infrastructure.Repositories;

namespace StayPilot.UnitTests
{
    /// <summary>
    /// A small, hand-placed dataset in a real SQL Server LocalDB database.
    /// It has to be a real database, not an in-memory one: the whole point of these tests is
    /// that the distance maths survives being translated into SQL, and an in-memory provider
    /// would just run the C# and pass even if SQL Server could not do it.
    ///
    /// Every listing is placed relative to central Quarteira and named after its role, so a
    /// failing test says what is actually wrong instead of naming a row number.
    /// </summary>
    public class ComparablesDatabaseFixture : IDisposable
    {
        private const string ConnectionString =
            @"Server=(localdb)\MSSQLLocalDB;Database=StayPilotComparablesTests;Trusted_Connection=True;TrustServerCertificate=True;";

        /// <summary>Central Quarteira: the property being valued sits here.</summary>
        public const decimal OriginLat = 37.069000m;

        /// <summary>Central Quarteira: the property being valued sits here.</summary>
        public const decimal OriginLon = -8.103000m;

        /// <summary>
        /// 0.021 degrees. Going north that is ~2338 m (outside a 2000 m radius); going east it
        /// is only ~1867 m (inside), because degrees of longitude are shorter at 37N. The pair
        /// is what proves the cos(latitude) scaling is really being applied.
        /// </summary>
        public const decimal AsymmetryOffset = 0.021000m;

        // Market areas.
        public int HomeMarketAreaId { get; private set; }
        public int NeighbourMarketAreaId { get; private set; }
        public int DistantMarketAreaId { get; private set; }

        // Listings, by the role each one plays in the tests.
        public int SameAreaButFarAwayId { get; private set; }
        public int North500Id { get; private set; }
        public int North1000Id { get; private set; }
        public int North1500Id { get; private set; }
        public int North2338Id { get; private set; }
        public int East1867Id { get; private set; }
        public int VillaNextDoorId { get; private set; }
        public int TwoBedroomNextDoorId { get; private set; }
        public int FourBedroomNextDoorId { get; private set; }
        public int StaleNextDoorId { get; private set; }
        public int DifferentTownId { get; private set; }
        public int NoCoordinatesId { get; private set; }

        public StayPilotDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<StayPilotDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;

            return new StayPilotDbContext(options);
        }

        public ComparablesDatabaseFixture()
        {
            using var context = CreateContext();

            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            // Market areas first, so the listings below can point at real ids.
            var home = MakeMarketArea("Quarteira Velha");
            var neighbour = MakeMarketArea("Praia de Quarteira");
            var distant = MakeMarketArea("Tavira Centro");

            context.MarketAreas.AddRange(home, neighbour, distant);
            context.SaveChanges();

            HomeMarketAreaId = home.Id;
            NeighbourMarketAreaId = neighbour.Id;
            DistantMarketAreaId = distant.Id;

            // In the property's own market area, but ~22 km away.
            var sameAreaButFarAway = MakeListing(home.Id, OriginLat + 0.200m, OriginLon);

            // Found by radius: 500 m, 1000 m and 1500 m north of the origin.
            var north500 = MakeListing(neighbour.Id, OriginLat + 0.004491m, OriginLon);
            var north1000 = MakeListing(neighbour.Id, OriginLat + 0.008983m, OriginLon);
            var north1500 = MakeListing(neighbour.Id, OriginLat + 0.013474m, OriginLon);

            // The cos(latitude) pair: same offset in degrees, different distance on the ground.
            var north2338 = MakeListing(neighbour.Id, OriginLat + AsymmetryOffset, OriginLon);
            var east1867 = MakeListing(neighbour.Id, OriginLat, OriginLon + AsymmetryOffset);

            // All ~111 m away, each failing one of the non-geographic filters.
            var villaNextDoor = MakeListing(neighbour.Id, OriginLat + 0.001m, OriginLon, propertyType: PropertyType.Villa);
            var twoBedroomNextDoor = MakeListing(neighbour.Id, OriginLat + 0.001m, OriginLon, typology: Typology.T2);
            var fourBedroomNextDoor = MakeListing(neighbour.Id, OriginLat + 0.001m, OriginLon, typology: Typology.T4);
            var staleNextDoor = MakeListing(neighbour.Id, OriginLat + 0.001m, OriginLon, monthsOld: 14);

            // Another town entirely, ~40 km away.
            var differentTown = MakeListing(distant.Id, 37.400000m, -7.650000m);

            // No coordinates at all, outside the property's market area: unreachable either way.
            var noCoordinates = MakeListing(neighbour.Id, null, null);

            context.PropertyListings.AddRange(
                sameAreaButFarAway, north500, north1000, north1500, north2338, east1867,
                villaNextDoor, twoBedroomNextDoor, fourBedroomNextDoor, staleNextDoor,
                differentTown, noCoordinates);

            context.SaveChanges();

            SameAreaButFarAwayId = sameAreaButFarAway.Id;
            North500Id = north500.Id;
            North1000Id = north1000.Id;
            North1500Id = north1500.Id;
            North2338Id = north2338.Id;
            East1867Id = east1867.Id;
            VillaNextDoorId = villaNextDoor.Id;
            TwoBedroomNextDoorId = twoBedroomNextDoor.Id;
            FourBedroomNextDoorId = fourBedroomNextDoor.Id;
            StaleNextDoorId = staleNextDoor.Id;
            DifferentTownId = differentTown.Id;
            NoCoordinatesId = noCoordinates.Id;
        }

        private static MarketArea MakeMarketArea(string zone)
        {
            return new MarketArea
            {
                Country = "Portugal",
                District = "Faro",
                Municipality = "Loulé",
                Town = "Quarteira",
                Zone = zone
            };
        }

        private static PropertyListing MakeListing(
            int marketAreaId,
            decimal? latitude,
            decimal? longitude,
            PropertyType propertyType = PropertyType.Apartment,
            Typology typology = Typology.T1,
            int monthsOld = 0)
        {
            // Unique per call, because SourceUrl has a unique index.
            var reference = Guid.NewGuid();

            return new PropertyListing
            {
                MarketAreaId = marketAreaId,
                PropertyType = propertyType,
                Typology = typology,
                SourceName = "test",
                SourceUrl = $"https://example.test/listing/{reference}",
                AreaM2 = 60,
                Latitude = latitude,
                Longitude = longitude,
                ListingSnapshots = new List<ListingSnapshot>
                {
                    new()
                    {
                        Price = 300_000m,
                        PricePerM2 = 5_000m,
                        Status = ListingStatus.Active,
                        SnapshotDateUtc = DateTime.UtcNow.AddMonths(-monthsOld)
                    }
                }
            };
        }

        public void Dispose()
        {
            using var context = CreateContext();
            context.Database.EnsureDeleted();

            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Tests for PropertyListingRepository.GetComparablePropertyListingAsync - the query that
    /// decides which listings a property is valued against.
    /// </summary>
    public class ComparablePropertyListingTests : IClassFixture<ComparablesDatabaseFixture>
    {
        private const int RadiusMeters = 2000;
        private const int Months = 12;
        private const int AreaM2 = 60;

        private readonly ComparablesDatabaseFixture _fixture;

        public ComparablePropertyListingTests(ComparablesDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        private async Task<List<PropertyListing>> FindComparablesAsync(
            bool withCoordinates = true,
            int radiusMeters = RadiusMeters,
            Typology typology = Typology.T1,
            int months = Months)
        {
            using var context = _fixture.CreateContext();
            var repository = new PropertyListingRepository(context);

            return await repository.GetComparablePropertyListingAsync(
                _fixture.HomeMarketAreaId,
                PropertyType.Apartment,
                typology,
                AreaM2,
                withCoordinates ? ComparablesDatabaseFixture.OriginLat : null,
                withCoordinates ? ComparablesDatabaseFixture.OriginLon : null,
                radiusMeters,
                months);
        }

        [Fact]
        public async Task ListingsInsideTheRadius_AreFound()
        {
            var comps = await FindComparablesAsync();

            // 500 m, 1000 m and 1500 m north are all well inside 2000 m.
            Assert.Contains(comps, x => x.Id == _fixture.North500Id);
            Assert.Contains(comps, x => x.Id == _fixture.North1000Id);
            Assert.Contains(comps, x => x.Id == _fixture.North1500Id);
        }

        [Fact]
        public async Task ListingsOutsideTheRadius_AreExcluded()
        {
            var comps = await FindComparablesAsync();

            Assert.DoesNotContain(comps, x => x.Id == _fixture.North2338Id);
            Assert.DoesNotContain(comps, x => x.Id == _fixture.DifferentTownId);
        }

        [Fact]
        public async Task LongitudeIsScaledByLatitude_SoTheSearchAreaIsACircleNotAnEllipse()
        {
            var comps = await FindComparablesAsync();

            // These two sit at the identical offset in DEGREES - one north, one east. North is
            // ~2338 m (out), east is ~1867 m (in). Drop the cos(latitude) scaling and both
            // measure the same, so this test is what actually guards that term.
            Assert.DoesNotContain(comps, x => x.Id == _fixture.North2338Id);
            Assert.Contains(comps, x => x.Id == _fixture.East1867Id);
        }

        [Fact]
        public async Task ListingsInTheSameMarketArea_AreIncludedEvenWhenFarOutsideTheRadius()
        {
            var comps = await FindComparablesAsync();

            // ~22 km away, but it is the property's own market area.
            Assert.Contains(comps, x => x.Id == _fixture.SameAreaButFarAwayId);
        }

        [Fact]
        public async Task SameMarketArea_IsRankedAheadOfEverythingFoundByRadius()
        {
            var comps = await FindComparablesAsync();

            // Even though it is the furthest away of all the matches.
            Assert.Equal(_fixture.SameAreaButFarAwayId, comps.First().Id);
        }

        [Fact]
        public async Task ListingsFoundByRadius_AreOrderedNearestFirst()
        {
            var comps = await FindComparablesAsync();

            var foundByRadius = comps
                .Where(x => x.MarketAreaId == _fixture.NeighbourMarketAreaId)
                .Select(x => x.Id)
                .ToList();

            var nearestFirst = new[]
            {
                _fixture.TwoBedroomNextDoorId, // ~111 m
                _fixture.North500Id,
                _fixture.North1000Id,
                _fixture.North1500Id,
                _fixture.East1867Id
            };

            Assert.Equal(nearestFirst, foundByRadius);
        }

        [Fact]
        public async Task TypologyWithinOneStep_IsAccepted()
        {
            var comps = await FindComparablesAsync();

            // A T2 is a fair comp for a T1.
            Assert.Contains(comps, x => x.Id == _fixture.TwoBedroomNextDoorId);
        }

        [Fact]
        public async Task TypologyMoreThanOneStepAway_IsRejected()
        {
            var comps = await FindComparablesAsync();

            // A T4 is not a comp for a T1, however close it is.
            Assert.DoesNotContain(comps, x => x.Id == _fixture.FourBedroomNextDoorId);
        }

        [Fact]
        public async Task ADifferentPropertyType_IsRejected()
        {
            var comps = await FindComparablesAsync();

            // A villa 111 m away is still not an apartment.
            Assert.DoesNotContain(comps, x => x.Id == _fixture.VillaNextDoorId);
        }

        [Fact]
        public async Task ListingsOlderThanTheCutoff_AreRejected()
        {
            var comps = await FindComparablesAsync();

            // Last seen 14 months ago, and the window is 12.
            Assert.DoesNotContain(comps, x => x.Id == _fixture.StaleNextDoorId);
        }

        [Fact]
        public async Task AWiderWindow_LetsOlderListingsBackIn()
        {
            var comps = await FindComparablesAsync(months: 24);

            Assert.Contains(comps, x => x.Id == _fixture.StaleNextDoorId);
        }

        [Fact]
        public async Task AWiderRadius_ReachesFurther()
        {
            var comps = await FindComparablesAsync(radiusMeters: 3000);

            // ~2338 m north: outside 2000 m, inside 3000 m.
            Assert.Contains(comps, x => x.Id == _fixture.North2338Id);
        }

        [Fact]
        public async Task NoCoordinates_FallsBackToTheMarketAreaAlone()
        {
            var comps = await FindComparablesAsync(withCoordinates: false);

            Assert.Single(comps);
            Assert.Equal(_fixture.SameAreaButFarAwayId, comps.Single().Id);
        }

        [Fact]
        public async Task NoCoordinates_DoesNotThrow()
        {
            // A property with no coordinates used to crash before the radius was even applied.
            var exception = await Record.ExceptionAsync(() => FindComparablesAsync(withCoordinates: false));

            Assert.Null(exception);
        }

        [Fact]
        public async Task AListingWithNoCoordinates_IsNeverFoundByRadius()
        {
            var comps = await FindComparablesAsync();

            Assert.DoesNotContain(comps, x => x.Id == _fixture.NoCoordinatesId);
        }

        [Fact]
        public async Task EveryComparable_ComesBackWithItsMarketAreaAndNewestSnapshot()
        {
            var comps = await FindComparablesAsync();

            // The valuation reads both straight off these objects, so a missing Include would
            // blow up later with a null reference instead of failing here.
            Assert.NotEmpty(comps);
            Assert.All(comps, x =>
            {
                Assert.NotNull(x.MarketArea);
                Assert.Single(x.ListingSnapshots);
            });
        }
    }
}
