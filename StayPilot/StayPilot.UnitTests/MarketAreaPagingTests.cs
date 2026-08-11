using Microsoft.EntityFrameworkCore;
using StayPilot.Application.Contracts.Request;
using StayPilot.Domain.Entities;
using StayPilot.Infrastructure.Persistence;
using StayPilot.Infrastructure.Repositories;

namespace StayPilot.UnitTests
{
    /// <summary>
    /// A small set of market areas in a real SQL Server LocalDB database.
    /// It has to be a real database, not an in-memory one: these tests are about what
    /// the paged query does once it becomes SQL (EF.Functions.Like, the ORDER BY that
    /// keeps pages stable, and COUNT before Skip/Take).
    ///
    /// The database also carries the seeded market areas of the real model, so every test
    /// row is named with the same made-up token and the tests search for it. That keeps the
    /// numbers below about the rows this fixture put there, not about the seed.
    /// </summary>
    public class MarketAreaPagingFixture : IDisposable
    {
        private const string ConnectionString =
            @"Server=(localdb)\MSSQLLocalDB;Database=StayPilotMarketAreaPagingTests;Trusted_Connection=True;TrustServerCertificate=True;";

        /// <summary>Made-up token in the district, municipality and town of every row this fixture adds.</summary>
        public const string Token = "Zzytest";

        /// <summary>How many market areas the fixture adds, all of them matching <see cref="Token"/>.</summary>
        public const int TestAreas = 25;

        /// <summary>Token used only in the two zone names, to tell a mid-name match apart from paging.</summary>
        public const string ZoneToken = "Zzyzone";

        /// <summary>How many of the rows have a zone containing <see cref="ZoneToken"/>.</summary>
        public const int ZoneMatches = 2;

        /// <summary>Every row in the database, seeded market areas included.</summary>
        public int TotalAreasInDatabase { get; private set; }

        public StayPilotDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<StayPilotDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;

            return new StayPilotDbContext(options);
        }

        public MarketAreaPagingFixture()
        {
            using var context = CreateContext();

            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            var areas = new List<MarketArea>
            {
                // Two zone rows, with the zone token in the middle and at the start of the name.
                MakeArea($"{Token}town 01", $"Praia de {ZoneToken}"),
                MakeArea($"{Token}town 02", $"{ZoneToken} Velha")
            };

            // Filler rows, named so their alphabetical order is obvious: Zzytown 03 .. Zzytown 25.
            for (var i = areas.Count + 1; i <= TestAreas; i++)
            {
                areas.Add(MakeArea($"{Token}town {i:00}"));
            }

            context.MarketAreas.AddRange(areas);
            context.SaveChanges();

            TotalAreasInDatabase = context.MarketAreas.Count();
        }

        private static MarketArea MakeArea(string town, string? zone = null)
        {
            return new MarketArea
            {
                Country = "Portugal",
                District = $"{Token}dist",
                Municipality = $"{Token}mun",
                Town = town,
                Zone = zone
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
    /// Tests for MarketAreaRepository.GetMarketAreasPageAsync — the paged query behind
    /// GET /api/MarketArea/GetAll, which the Market Areas screen walks page by page.
    /// </summary>
    public class MarketAreaPagingTests : IClassFixture<MarketAreaPagingFixture>
    {
        private readonly MarketAreaPagingFixture _fixture;

        public MarketAreaPagingTests(MarketAreaPagingFixture fixture)
        {
            _fixture = fixture;
        }

        // Every test but the last one asks only for this fixture's rows.
        private async Task<(List<MarketArea> Items, int TotalRecords)> GetTestRowsPageAsync(int pageNumber, int pageSize)
        {
            return await GetPageAsync(new MarketAreaRequest
            {
                Search = $"{MarketAreaPagingFixture.Token}town",
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }

        private async Task<(List<MarketArea> Items, int TotalRecords)> GetPageAsync(MarketAreaRequest request)
        {
            using var context = _fixture.CreateContext();
            var repository = new MarketAreaRepository(context);

            return await repository.GetMarketAreasPageAsync(request);
        }

        [Fact]
        public async Task FirstPage_ReturnsOnlyThatPage_ButCountsEveryMatch()
        {
            var (items, totalRecords) = await GetTestRowsPageAsync(pageNumber: 1, pageSize: 10);

            Assert.Equal(10, items.Count);
            Assert.Equal(MarketAreaPagingFixture.TestAreas, totalRecords);
        }

        [Fact]
        public async Task LastPage_ReturnsTheRemainder()
        {
            // 25 matches in pages of 10 -> page 3 holds the last 5.
            var (items, totalRecords) = await GetTestRowsPageAsync(pageNumber: 3, pageSize: 10);

            Assert.Equal(5, items.Count);
            Assert.Equal(MarketAreaPagingFixture.TestAreas, totalRecords);
        }

        [Fact]
        public async Task PageBeyondTheEnd_ReturnsNothing_AndStillCounts()
        {
            var (items, totalRecords) = await GetTestRowsPageAsync(pageNumber: 9, pageSize: 10);

            Assert.Empty(items);
            Assert.Equal(MarketAreaPagingFixture.TestAreas, totalRecords);
        }

        [Fact]
        public async Task Pages_DoNotOverlap_AndCoverEveryMatch()
        {
            // The point of the ORDER BY in the query: without a stable order the same row
            // can come back on two pages and another row is never seen at all.
            var (first, _) = await GetTestRowsPageAsync(pageNumber: 1, pageSize: 10);
            var (second, _) = await GetTestRowsPageAsync(pageNumber: 2, pageSize: 10);
            var (third, _) = await GetTestRowsPageAsync(pageNumber: 3, pageSize: 10);

            var ids = first.Concat(second).Concat(third).Select(x => x.Id).ToList();

            Assert.Equal(MarketAreaPagingFixture.TestAreas, ids.Count);
            Assert.Equal(MarketAreaPagingFixture.TestAreas, ids.Distinct().Count());
        }

        [Fact]
        public async Task Search_MatchesAnyPartOfTheName()
        {
            // The token sits in the middle of "Praia de Zzyzone", so this only passes if the
            // query is a contains match, not a starts-with one.
            var (items, totalRecords) = await GetPageAsync(new MarketAreaRequest { Search = MarketAreaPagingFixture.ZoneToken });

            Assert.Equal(MarketAreaPagingFixture.ZoneMatches, totalRecords);
            Assert.All(items, area => Assert.Contains(MarketAreaPagingFixture.ZoneToken, area.Zone));
        }

        [Fact]
        public async Task Search_AlsoLooksAtDistrictAndMunicipality()
        {
            // The municipality token is on every row this fixture added, and on none of the seeded ones.
            var (_, totalRecords) = await GetPageAsync(new MarketAreaRequest { Search = $"{MarketAreaPagingFixture.Token}mun" });

            Assert.Equal(MarketAreaPagingFixture.TestAreas, totalRecords);
        }

        [Fact]
        public async Task Search_ThatMatchesNothing_ReturnsAnEmptyPage()
        {
            var (items, totalRecords) = await GetPageAsync(new MarketAreaRequest { Search = "Reykjavik" });

            Assert.Empty(items);
            Assert.Equal(0, totalRecords);
        }

        [Fact]
        public async Task BlankSearch_IsIgnored_SoEveryRowCounts()
        {
            var (_, totalRecords) = await GetPageAsync(new MarketAreaRequest { Search = "   " });

            Assert.Equal(_fixture.TotalAreasInDatabase, totalRecords);
        }
    }
}
