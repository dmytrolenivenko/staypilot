using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Helpers.Calculators;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Application.Services;
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;

namespace StayPilot.UnitTests
{
    // Hand-written fake - this project has no Moq dependency (see PropertyListingServiceBulkAddTests).
    // Only the one method the overview reads is implemented; the rest is not this test's business.
    file class FakeOverviewListingRepo : IPropertyListingRepository
    {
        private readonly List<PropertyListing> _listings;

        public FakeOverviewListingRepo(List<PropertyListing> listings) => _listings = listings;

        public Task<List<PropertyListing>> GetListingsForMarketOverviewAsync(string? district, string? municipality, string? town, PropertyType? propertyType, Typology? typology) =>
            Task.FromResult(_listings);

        public Task<List<PropertyListing>> GetListingsWithHistoryAsync(string? district, string? municipality, string? town) => throw new NotImplementedException();

        public Task<PropertyListing?> GetPropertyListingByIdAsync(int id) => throw new NotImplementedException();
        public Task<List<PropertyListing>?> GetBulkPropertyListingByUrlAsync(List<string> urls) => throw new NotImplementedException();
        public Task<PropertyListing> AddPropertyListingAsync(PropertyListing propertyListing) => throw new NotImplementedException();
        public Task<(List<PropertyListing> Items, int TotalRecords)> FilterPropertyAsync(FilterPropertyListingRequest request) => throw new NotImplementedException();
        public Task SaveChangesAsync() => throw new NotImplementedException();
        public void DiscardPendingChanges() => throw new NotImplementedException();
        public Task<List<PropertyListing>> GetComparablePropertyListingAsync(int marketId, PropertyType propertyType, Typology typology, int areaM2, int? distanceToBeachMeters, decimal? latitude, decimal? longitude, int radiusMeters, int months) => throw new NotImplementedException();
        public Task<List<PropertyListing>> GetAllListingsForFeaturePremiumCalculationAsync() => throw new NotImplementedException();
    }

    /// <summary>
    /// The overview has three things worth testing: it summarises a slice four ways, it draws a
    /// distribution one freak listing cannot flatten, and it throws away what it cannot measure.
    /// The service adds one more: the name it puts on the slice.
    /// </summary>
    public class MarketOverviewTests
    {
        [Fact]
        public void Calculate_OneVeryExpensiveVilla_MovesTheAverageButNotTheMedian()
        {
            var listings = new List<PropertyListing>
            {
                Listing(200_000m, 100),
                Listing(220_000m, 100),
                Listing(240_000m, 100),
                Listing(5_000_000m, 100)
            };

            var overview = MarketOverviewCalculator.Calculate(listings, 10);

            // The gap between these two IS the reading, which is why both are on the screen.
            Assert.Equal(230_000m, overview.Price.Median);
            Assert.Equal(1_415_000m, overview.Price.Average);
            Assert.Equal(200_000m, overview.Price.Min);
            Assert.Equal(5_000_000m, overview.Price.Max);
            Assert.Equal(4, overview.ListingCount);
        }

        [Fact]
        public void Calculate_ListingsItCannotMeasure_AreLeftOutOfTheCount()
        {
            var noSnapshot = Listing(200_000m, 100);
            noSnapshot.ListingSnapshots.Clear();

            var listings = new List<PropertyListing>
            {
                Listing(200_000m, 100),
                noSnapshot,
                Listing(0m, 100),   // no price
                Listing(200_000m, 0) // no area, so no price per m2 either
            };

            var overview = MarketOverviewCalculator.Calculate(listings, 10);

            // One measurable listing out of four. The count must not promise the other three.
            Assert.Equal(1, overview.ListingCount);
        }

        [Fact]
        public void Calculate_NothingMatched_IsAnEmptyAnswerAndNotAnError()
        {
            var overview = MarketOverviewCalculator.Calculate(new List<PropertyListing>(), 10);

            Assert.True(overview.Succeeded);
            Assert.Equal(0, overview.ListingCount);
            Assert.Empty(overview.Distribution);
            Assert.Empty(overview.Typologies);
            Assert.Equal(0m, overview.Price.Median);
        }

        [Fact]
        public void Calculate_Distribution_CountsEveryListingAndSharesAddUpToAHundred()
        {
            var listings = new List<PropertyListing>();

            for (var price = 100_000m; price <= 1_000_000m; price += 100_000m)
            {
                listings.Add(Listing(price, 100));
            }

            var overview = MarketOverviewCalculator.Calculate(listings, 4);

            Assert.Equal(4, overview.Distribution.Count);
            Assert.Equal(10, overview.Distribution.Sum(x => x.ListingCount));
            Assert.Equal(100m, overview.Distribution.Sum(x => x.SharePercent));

            // The edge bars report the real cheapest and dearest price, not the percentile, so the
            // range on screen still matches the min and max shown above it.
            Assert.Equal(overview.Price.Min, overview.Distribution[0].FromPrice);
            Assert.Equal(overview.Price.Max, overview.Distribution[^1].ToPrice);
        }

        [Fact]
        public void Calculate_OneFreakListing_DoesNotEmptyTheOtherBars()
        {
            // Forty flats between 200k and 278k, plus one five-million villa. Bars drawn from the
            // raw min and max would put the forty in the first bar and leave eight of ten empty.
            var listings = new List<PropertyListing>();

            for (var price = 200_000m; price < 280_000m; price += 2_000m)
            {
                listings.Add(Listing(price, 100));
            }

            listings.Add(Listing(5_000_000m, 100));

            var overview = MarketOverviewCalculator.Calculate(listings, 10);

            Assert.Equal(10, overview.Distribution.Count);
            Assert.All(overview.Distribution, bucket => Assert.True(bucket.ListingCount > 0));

            // The villa is counted, in the last bar - trimmed from the widths, never from the counts.
            Assert.Equal(41, overview.Distribution.Sum(x => x.ListingCount));
            Assert.Equal(5_000_000m, overview.Distribution[^1].ToPrice);
        }

        [Fact]
        public void Calculate_EveryListingAsksTheSame_DrawsOneBar()
        {
            var listings = new List<PropertyListing>
            {
                Listing(250_000m, 100),
                Listing(250_000m, 100),
                Listing(250_000m, 100)
            };

            var overview = MarketOverviewCalculator.Calculate(listings, 10);

            var only = Assert.Single(overview.Distribution);
            Assert.Equal(3, only.ListingCount);
            Assert.Equal(100m, only.SharePercent);
        }

        [Fact]
        public void Calculate_BucketCountBelowTheMinimum_IsClampedInsteadOfDividingByZero()
        {
            var listings = new List<PropertyListing>
            {
                Listing(100_000m, 100),
                Listing(200_000m, 100),
                Listing(300_000m, 100)
            };

            var overview = MarketOverviewCalculator.Calculate(listings, 0);

            Assert.Equal(4, overview.Distribution.Count);
        }

        [Fact]
        public void Calculate_TypologyRows_OneRowPerLayoutFewestRoomsFirst()
        {
            var listings = new List<PropertyListing>
            {
                Listing(300_000m, 100, Typology.T3),
                Listing(200_000m, 80, Typology.T2),
                Listing(220_000m, 80, Typology.T2),
                Listing(240_000m, 80, Typology.T2)
            };

            var overview = MarketOverviewCalculator.Calculate(listings, 10);

            Assert.Equal(new[] { Typology.T2, Typology.T3 }, overview.Typologies.Select(x => x.Typology));

            var t2 = overview.Typologies[0];
            Assert.Equal(3, t2.ListingCount);
            Assert.Equal(220_000m, t2.MedianPrice);
            Assert.Equal(80m, t2.MedianAreaM2);
            Assert.Equal(2_750m, t2.MedianPricePerM2);

            // The single T3 is kept, not gated away: its count is on screen next to its price, and
            // "there is one T3 here at all" is part of what the overview answers.
            Assert.Equal(1, overview.Typologies[1].ListingCount);
        }

        [Fact]
        public async Task GetMarketOverviewAsync_NamesTheSliceByItsNarrowestPart()
        {
            var service = new MarketOverviewService(new FakeOverviewListingRepo(new List<PropertyListing> { Listing(200_000m, 100) }));

            var town = await service.GetMarketOverviewAsync(new MarketOverviewRequest
            {
                District = "Faro",
                Municipality = "Albufeira",
                Town = "Guia"
            });

            var municipality = await service.GetMarketOverviewAsync(new MarketOverviewRequest
            {
                District = "Faro",
                Municipality = "Albufeira"
            });

            var everywhere = await service.GetMarketOverviewAsync(new MarketOverviewRequest());

            Assert.Equal("Guia (Albufeira)", town.PlaceName);
            Assert.Equal("Albufeira (Faro)", municipality.PlaceName);
            Assert.Equal("All areas", everywhere.PlaceName);
        }

        [Fact]
        public void Calculate_BrokenDownByDistrict_RanksThePlacesAndComparesEachToTheSlice()
        {
            var listings = new List<PropertyListing>
            {
                Placed(400_000m, 100, "Faro", "Albufeira", "Guia"),
                Placed(400_000m, 100, "Faro", "Albufeira", "Guia"),
                Placed(200_000m, 100, "Beja", "Moura", "Amareleja"),
                Placed(200_000m, 100, "Beja", "Moura", "Amareleja")
            };

            var overview = MarketOverviewCalculator.Calculate(listings, 10, AreaLevel.District);

            Assert.NotNull(overview.Breakdown);
            Assert.Equal(AreaLevel.District, overview.Breakdown!.Level);

            // Dearest per square meter first, so the row that moves the slice sits at the top.
            Assert.Equal(new[] { "Faro", "Beja" }, overview.Breakdown.Items.Select(x => x.DisplayName));

            var faro = overview.Breakdown.Items[0];
            Assert.Equal(2, faro.ListingCount);
            Assert.Equal(50m, faro.SharePercent);
            Assert.Equal(4_000m, faro.MedianPricePerM2);

            // The slice median is 3,000/m2 across all four, so Faro is a third above its own slice.
            // This is the column the screen exists for - the raw price needs arithmetic, this does not.
            Assert.Equal(33.3m, faro.VsSlicePercent);
            Assert.Equal(-33.3m, overview.Breakdown.Items[1].VsSlicePercent);
        }

        [Fact]
        public void Calculate_ListingsWithNoArea_AreLeftOutOfTheBreakdownEntirely()
        {
            var listings = new List<PropertyListing>
            {
                Placed(400_000m, 100, "Faro", "Albufeira", "Guia"),
                Listing(200_000m, 100)
            };

            var overview = MarketOverviewCalculator.Calculate(listings, 10, AreaLevel.District);

            // The unplaced listing still counts towards the slice - it has a price and a size, so
            // it is real evidence. It just is not a place, and a row named "" would read as one.
            Assert.Equal(2, overview.ListingCount);
            Assert.Single(overview.Breakdown!.Items);
            Assert.Equal("Faro", overview.Breakdown.Items[0].DisplayName);
        }

        [Fact]
        public void Calculate_WithNoBreakdownLevel_ReturnsNoBreakdown()
        {
            var overview = MarketOverviewCalculator.Calculate(
                new List<PropertyListing> { Placed(400_000m, 100, "Faro", "Albufeira", "Guia") }, 10);

            Assert.Null(overview.Breakdown);
        }

        [Fact]
        public async Task GetMarketOverviewAsync_BreaksTheSliceIntoTheGrainBelowIt()
        {
            var service = new MarketOverviewService(new FakeOverviewListingRepo(
                new List<PropertyListing> { Placed(400_000m, 100, "Faro", "Albufeira", "Guia") }));

            var everywhere = await service.GetMarketOverviewAsync(new MarketOverviewRequest());
            var district = await service.GetMarketOverviewAsync(new MarketOverviewRequest { District = "Faro" });
            var municipality = await service.GetMarketOverviewAsync(
                new MarketOverviewRequest { District = "Faro", Municipality = "Albufeira" });
            var town = await service.GetMarketOverviewAsync(
                new MarketOverviewRequest { District = "Faro", Municipality = "Albufeira", Town = "Guia" });

            Assert.Equal(AreaLevel.District, everywhere.Breakdown!.Level);
            Assert.Equal(AreaLevel.Municipality, district.Breakdown!.Level);
            Assert.Equal(AreaLevel.Town, municipality.Breakdown!.Level);

            // A freguesia is the finest grain we hold, so there is nothing left to cut it into.
            Assert.Null(town.Breakdown);
        }

        /// <summary>
        /// A listing that also knows where it is, for the breakdown. Same shape as
        /// <see cref="Listing"/> otherwise.
        /// </summary>
        private static PropertyListing Placed(
            decimal price, int areaM2, string district, string municipality, string town)
        {
            var listing = Listing(price, areaM2);

            listing.MarketArea = new MarketArea
            {
                District = district,
                Municipality = municipality,
                Town = town
            };

            return listing;
        }

        /// <summary>
        /// One listing with one snapshot at the given price. The price for each square meter is
        /// worked out here the same way the importer does it, so the maths under test is the only
        /// thing the assertions can be measuring.
        /// </summary>
        private static PropertyListing Listing(decimal price, int areaM2, Typology typology = Typology.T2)
        {
            return new PropertyListing
            {
                AreaM2 = areaM2,
                Typology = typology,
                PropertyType = PropertyType.Apartment,
                ListingSnapshots = new List<ListingSnapshot>
                {
                    new()
                    {
                        Price = price,
                        PricePerM2 = areaM2 == 0 ? 0m : price / areaM2,
                        SnapshotDateUtc = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
                        Status = ListingStatus.Active
                    }
                }
            };
        }
    }
}
