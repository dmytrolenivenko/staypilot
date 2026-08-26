using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Helpers.Mappers;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Application.Services;
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;

namespace StayPilot.UnitTests
{
    // Hand-written fake - this project has no Moq dependency. It applies the same scope and
    // sample gate the real repository does, because those rules are half of what is under test.
    file class FakeStatsRepo : IMarketAreaStatsRepository
    {
        private readonly List<MarketAreaStats> _rows;

        public FakeStatsRepo(List<MarketAreaStats> rows) => _rows = rows;

        public Task<List<MarketAreaStats>> GetLeaderboardAsync(
            AreaLevel level, int minListings, string? district = null, string? municipality = null) =>
            Task.FromResult(Scoped(level, minListings, district, municipality));

        public Task<List<MarketAreaStats>> GetWithTypologiesAsync(
            AreaLevel level, int minListings, string? district = null, string? municipality = null) =>
            Task.FromResult(Scoped(level, minListings, district, municipality));

        private List<MarketAreaStats> Scoped(
            AreaLevel level, int minListings, string? district, string? municipality) =>
            _rows
                .Where(x => x.Level == level && x.ListingCount >= minListings)
                .Where(x => string.IsNullOrWhiteSpace(district) || x.District == district)
                .Where(x => string.IsNullOrWhiteSpace(municipality) || x.Municipality == municipality)
                .ToList();

        public Task<List<MarketAreaStats>> GetAllMarketAreaStatsAsync() => throw new NotImplementedException();
        public Task AddMarketAreaStatsAsync(IEnumerable<MarketAreaStats> stats) => throw new NotImplementedException();
        public void RemoveMarketAreaStats(IEnumerable<MarketAreaStats> stats) => throw new NotImplementedException();
        public Task SaveChangesAsync() => throw new NotImplementedException();
    }

    // The stats service takes a listing repository too, but only the recalculation path uses it.
    file class UnusedListingRepo : IPropertyListingRepository
    {
        public Task<PropertyListing?> GetPropertyListingByIdAsync(int id) => throw new NotImplementedException();
        public Task<List<PropertyListing>?> GetBulkPropertyListingByUrlAsync(List<string> urls) => throw new NotImplementedException();
        public Task<PropertyListing> AddPropertyListingAsync(PropertyListing propertyListing) => throw new NotImplementedException();
        public Task<(List<PropertyListing> Items, int TotalRecords)> FilterPropertyAsync(FilterPropertyListingRequest request) => throw new NotImplementedException();
        public Task SaveChangesAsync() => throw new NotImplementedException();
        public void DiscardPendingChanges() => throw new NotImplementedException();
        public Task<List<PropertyListing>> GetComparablePropertyListingAsync(int marketId, PropertyType propertyType, Typology typology, int areaM2, int? distanceToBeachMeters, decimal latitude, decimal longitude, int radiusMeters, int months) => throw new NotImplementedException();
        public Task<List<PropertyListing>> GetAllListingsForFeaturePremiumCalculationAsync() => throw new NotImplementedException();
        public Task<List<MarketAreaStatsListingRow>> GetAllListingsForMarketAreaStatsAsync() => throw new NotImplementedException();
        public Task<List<MarketOverviewListingRow>> GetListingsForMarketOverviewAsync(string? district, string? municipality, string? town, PropertyType? propertyType, Typology? typology) => throw new NotImplementedException();

        public Task<List<PropertyListing>> GetListingsWithHistoryAsync(string? district, string? municipality, string? town) => throw new NotImplementedException();
    }

    /// <summary>
    /// The read side of the market area stats: what a budget reaches, which places count as
    /// neighbours, and how far the renovation discount deserves to be trusted.
    /// </summary>
    public class MarketAreaStatsServiceTests
    {
        // --- What money buys ------------------------------------------------

        [Fact]
        public async Task GetBudgetRankingAsync_MinTypology_DropsPlacesWhereTheBudgetFallsShort()
        {
            var service = Service(new List<MarketAreaStats>
            {
                Place("Faro", "Albufeira", typologies: (Typology.T1, 180_000m)),
                Place("Faro", "Loule", typologies: (Typology.T3, 250_000m))
            });

            var response = await service.GetBudgetRankingAsync(new MarketAreaBudgetRequest
            {
                Budget = 300_000m,
                MinTypology = Typology.T3
            });

            // Albufeira is affordable, but a T1 is not an answer to "where does 300k buy a T3".
            Assert.Single(response.Items);
            Assert.Equal("Loule", response.Items[0].Municipality);
        }

        [Fact]
        public async Task GetBudgetRankingAsync_Stretch_ReachesFurtherAndSaysSo()
        {
            var service = Service(new List<MarketAreaStats>
            {
                Place("Faro", "Albufeira", typologies: (Typology.T2, 320_000m))
            });

            var strict = await service.GetBudgetRankingAsync(new MarketAreaBudgetRequest { Budget = 300_000m });

            var stretched = await service.GetBudgetRankingAsync(new MarketAreaBudgetRequest
            {
                Budget = 300_000m,
                StretchPercent = 10
            });

            // Nothing over budget is affordable unless you say so.
            Assert.Empty(strict.Items);

            Assert.Single(stretched.Items);
            Assert.Equal(330_000m, stretched.Reach);

            // Reached, but flagged - "you could have this for a little more" is worth knowing, and
            // worth knowing that it is what you are looking at.
            Assert.True(stretched.Items[0].NeedsStretch);
        }

        [Fact]
        public async Task GetBudgetRankingAsync_ListsEveryTypologyWithinReach_NotOnlyTheBiggest()
        {
            var service = Service(new List<MarketAreaStats>
            {
                Place("Faro", "Albufeira", typologies: new[]
                {
                    (Typology.T1, 120_000m),
                    (Typology.T2, 200_000m),
                    (Typology.T3, 280_000m),
                    (Typology.T4, 900_000m)
                })
            });

            var response = await service.GetBudgetRankingAsync(new MarketAreaBudgetRequest { Budget = 300_000m });

            // The headline stays "the most rooms your money buys"...
            Assert.Equal(Typology.T3, response.Items[0].BestTypology);

            // ...but the trade it hides - a bigger, cheaper-per-metre T2 - is on the row too, and
            // the T4 nobody can afford is not.
            Assert.Equal(
                new[] { Typology.T3, Typology.T2, Typology.T1 },
                response.Items[0].AffordableTypologies.Select(x => x.Typology));
        }

        [Fact]
        public async Task GetBudgetRankingAsync_ScopedToADistrict_LeavesTheRestOfTheCountryOut()
        {
            var service = Service(new List<MarketAreaStats>
            {
                Place("Faro", "Albufeira", typologies: (Typology.T2, 200_000m)),
                Place("Beja", "Moura", typologies: (Typology.T3, 90_000m))
            });

            var response = await service.GetBudgetRankingAsync(new MarketAreaBudgetRequest
            {
                Budget = 300_000m,
                District = "Faro"
            });

            Assert.Single(response.Items);
            Assert.Equal("Albufeira", response.Items[0].Municipality);
        }

        [Fact]
        public async Task GetBudgetRankingAsync_CarriesTheGrainEachRowMeasures()
        {
            var service = Service(new List<MarketAreaStats>
            {
                Place("Faro", "Albufeira", typologies: (Typology.T2, 200_000m))
            });

            var response = await service.GetBudgetRankingAsync(new MarketAreaBudgetRequest { Budget = 300_000m });

            // Left unset this defaults to 0, which is not a level at all - the screen would then
            // label a município row "Distrito" and serialise a number where a name belongs.
            Assert.Equal(AreaLevel.Municipality, response.Items[0].Level);
        }

        // --- Neighbour gaps -------------------------------------------------

        [Fact]
        public async Task GetNeighbourGapsAsync_ComparedOnOneTypology_UsesThatTypologysPrices()
        {
            // All stock says these two are 50% apart. Their T2s are only 10% apart, so the
            // headline gap is about what each place sells, not about the places.
            var albufeira = Place("Faro", "Albufeira", pricePerM2: 4_000m, latitude: 37.09m, longitude: -8.25m,
                typologies: (Typology.T2, 300_000m));
            var loule = Place("Faro", "Loule", pricePerM2: 2_000m, latitude: 37.14m, longitude: -8.02m,
                typologies: (Typology.T2, 270_000m));

            SetTypologyPricePerM2(albufeira, Typology.T2, 3_000m);
            SetTypologyPricePerM2(loule, Typology.T2, 2_700m);

            var service = Service(new List<MarketAreaStats> { albufeira, loule });

            var allStock = await service.GetNeighbourGapsAsync(new MarketAreaNeighbourGapRequest
            {
                MinGapPercent = 20
            });

            var t2Only = await service.GetNeighbourGapsAsync(new MarketAreaNeighbourGapRequest
            {
                MinGapPercent = 20,
                Typology = Typology.T2
            });

            Assert.Single(allStock.Items);
            Assert.Equal(50m, allStock.Items[0].GapPercent);

            // Compared like with like, the pair does not clear the 20% floor at all.
            Assert.Empty(t2Only.Items);
            Assert.Equal(Typology.T2, t2Only.ComparedOn);
        }

        [Fact]
        public async Task GetNeighbourGapsAsync_ComparedOnOneTypology_KeepsTheAllStockPriceForContext()
        {
            var albufeira = Place("Faro", "Albufeira", pricePerM2: 4_000m, latitude: 37.09m, longitude: -8.25m,
                typologies: (Typology.T2, 300_000m));
            var loule = Place("Faro", "Loule", pricePerM2: 3_900m, latitude: 37.14m, longitude: -8.02m,
                typologies: (Typology.T2, 200_000m));

            SetTypologyPricePerM2(albufeira, Typology.T2, 5_000m);
            SetTypologyPricePerM2(loule, Typology.T2, 2_000m);

            var service = Service(new List<MarketAreaStats> { albufeira, loule });

            var response = await service.GetNeighbourGapsAsync(new MarketAreaNeighbourGapRequest
            {
                MinGapPercent = 20,
                Typology = Typology.T2
            });

            var gap = Assert.Single(response.Items);

            // The compared-on price drives the gap...
            Assert.Equal(5_000m, gap.Expensive.MedianPricePerM2);

            // ...and the all-stock price rides along, because the two together are the finding:
            // T2s are 60% apart while the places overall are within 3%.
            Assert.Equal(4_000m, gap.Expensive.AllStockPricePerM2);
            Assert.Equal(3_900m, gap.Cheaper.AllStockPricePerM2);
        }

        [Fact]
        public async Task GetNeighbourGapsAsync_TooFewOfThatTypology_LeavesThePlaceOut()
        {
            var albufeira = Place("Faro", "Albufeira", pricePerM2: 4_000m, latitude: 37.09m, longitude: -8.25m,
                typologies: (Typology.T4, 900_000m));
            var loule = Place("Faro", "Loule", pricePerM2: 2_000m, latitude: 37.14m, longitude: -8.02m,
                typologies: (Typology.T4, 400_000m));

            // Loule has three T4 adverts. A "T4 gap" measured off three adverts is not a finding.
            loule.TypologyStats[0].ListingCount = 3;

            var service = Service(new List<MarketAreaStats> { albufeira, loule });

            var response = await service.GetNeighbourGapsAsync(new MarketAreaNeighbourGapRequest
            {
                MinGapPercent = 20,
                Typology = Typology.T4,
                MinTypologyListings = 5
            });

            Assert.Empty(response.Items);
        }

        [Fact]
        public async Task GetNeighbourGapsAsync_PlacesTooFarApart_AreNotNeighbours()
        {
            var service = Service(new List<MarketAreaStats>
            {
                Place("Faro", "Albufeira", pricePerM2: 4_000m, latitude: 37.09m, longitude: -8.25m),
                Place("Braga", "Braga", pricePerM2: 1_500m, latitude: 41.55m, longitude: -8.42m)
            });

            var response = await service.GetNeighbourGapsAsync(new MarketAreaNeighbourGapRequest
            {
                MaxDistanceKm = 25
            });

            Assert.Empty(response.Items);
        }

        // --- Renovation evidence --------------------------------------------

        [Fact]
        public void MapToResponse_SeparatedSpreadsAndEnoughProjects_ReadsAsHighConfidence()
        {
            var stats = WithRenovation(
                projectCount: 40, projectP25: 1_000m, projectMedian: 1_200m, projectP75: 1_400m,
                moveInCount: 60, moveInP25: 2_000m, moveInMedian: 2_300m, moveInP75: 2_600m,
                listingCount: 120);

            var evidence = Converter.MapToResponse(stats).RenovationEvidence;

            Assert.NotNull(evidence);
            Assert.Equal(ValuationConfidence.High, evidence!.Confidence);

            // The two middle halves do not touch, so the discount is a real separation.
            Assert.Equal(0m, evidence.SpreadOverlapPercent);
        }

        [Fact]
        public void MapToResponse_SpreadsSittingOnTopOfEachOther_ReadsAsLowConfidence()
        {
            var stats = WithRenovation(
                projectCount: 40, projectP25: 1_000m, projectMedian: 1_500m, projectP75: 2_000m,
                moveInCount: 60, moveInP25: 1_100m, moveInMedian: 1_700m, moveInP75: 2_100m,
                listingCount: 120);

            var evidence = Converter.MapToResponse(stats).RenovationEvidence;

            // The medians differ by 200, but most project stock here asks what finished stock
            // asks - that difference is noise wearing a decimal point.
            Assert.Equal(ValuationConfidence.Low, evidence!.Confidence);
            Assert.Contains("overlap", evidence.Reason);
        }

        [Fact]
        public void MapToResponse_TooFewProjects_SaysSoBeforeAnythingElse()
        {
            var stats = WithRenovation(
                projectCount: 4, projectP25: 1_000m, projectMedian: 1_200m, projectP75: 1_400m,
                moveInCount: 60, moveInP25: 2_000m, moveInMedian: 2_300m, moveInP75: 2_600m,
                listingCount: 120);

            var evidence = Converter.MapToResponse(stats).RenovationEvidence;

            Assert.Equal(ValuationConfidence.Low, evidence!.Confidence);
            Assert.Contains("4 project", evidence.Reason);
        }

        [Fact]
        public void MapToResponse_DiscountRestingOnASliverOfTheStock_IsOnlyMediumConfidence()
        {
            // Clean separation and plenty of projects, but 900 of the 1,000 listings here carry
            // neither a condition nor a certificate.
            var stats = WithRenovation(
                projectCount: 40, projectP25: 1_000m, projectMedian: 1_200m, projectP75: 1_400m,
                moveInCount: 60, moveInP25: 2_000m, moveInMedian: 2_300m, moveInP75: 2_600m,
                listingCount: 1_000);

            var evidence = Converter.MapToResponse(stats).RenovationEvidence;

            Assert.Equal(ValuationConfidence.Medium, evidence!.Confidence);
            Assert.Equal(10m, evidence.ClassifiedSharePercent);
        }

        [Fact]
        public void MapToResponse_OnlyOneSideMeasured_HasNoEvidenceAtAll()
        {
            var stats = Place("Faro", "Albufeira");
            stats.ProjectMedianPricePerM2 = 1_200m;
            stats.MoveInMedianPricePerM2 = null;

            var response = Converter.MapToResponse(stats);

            // No discount to judge, so no verdict is invented for one.
            Assert.Null(response.RenovationDiscountPerM2);
            Assert.Null(response.RenovationEvidence);
        }

        [Fact]
        public void MapToResponse_MediansPresentButNoSpread_IsTreatedAsFullyOverlapping()
        {
            var stats = Place("Faro", "Albufeira");
            stats.ProjectCount = 40;
            stats.ProjectMedianPricePerM2 = 1_200m;
            stats.MoveInCount = 60;
            stats.MoveInMedianPricePerM2 = 2_300m;

            var evidence = Converter.MapToResponse(stats).RenovationEvidence;

            // A missing measurement must never read as a clean separation - that would turn the
            // least evidenced rows into the most confident ones.
            Assert.Equal(100m, evidence!.SpreadOverlapPercent);
            Assert.Equal(ValuationConfidence.Low, evidence.Confidence);
        }

        // --- Helpers ---------------------------------------------------------

        private static MarketAreaStatsService Service(List<MarketAreaStats> rows)
        {
            return new MarketAreaStatsService(new FakeStatsRepo(rows), new UnusedListingRepo());
        }

        /// <summary>
        /// One municipio row. The typologies are given as (typology, median price) pairs and the
        /// price for each square meter is derived from a 100m2 flat, so the two never disagree.
        /// </summary>
        private static MarketAreaStats Place(
            string district,
            string municipality,
            decimal pricePerM2 = 2_000m,
            int listingCount = 50,
            decimal? latitude = null,
            decimal? longitude = null,
            params (Typology Typology, decimal MedianPrice)[] typologies)
        {
            return new MarketAreaStats
            {
                Level = AreaLevel.Municipality,
                District = district,
                Municipality = municipality,
                ListingCount = listingCount,
                MedianPricePerM2 = pricePerM2,
                MedianAreaM2 = 100m,
                CentroidLatitude = latitude,
                CentroidLongitude = longitude,
                TypologyStats = typologies
                    .Select(x => new MarketAreaTypologyStats
                    {
                        Typology = x.Typology,
                        MedianPrice = x.MedianPrice,
                        MedianAreaM2 = 100m,
                        MedianPricePerM2 = x.MedianPrice / 100m,
                        ListingCount = 20
                    })
                    .ToList()
            };
        }

        /// <summary>
        /// Breaks the derived price for one typology on purpose, so a test can set up a place
        /// whose typology price and all-stock price disagree - which is the whole point of being
        /// able to compare on one typology.
        /// </summary>
        private static void SetTypologyPricePerM2(MarketAreaStats place, Typology typology, decimal pricePerM2)
        {
            place.TypologyStats.Single(x => x.Typology == typology).MedianPricePerM2 = pricePerM2;
        }

        private static MarketAreaStats WithRenovation(
            int projectCount, decimal projectP25, decimal projectMedian, decimal projectP75,
            int moveInCount, decimal moveInP25, decimal moveInMedian, decimal moveInP75,
            int listingCount)
        {
            var stats = Place("Faro", "Albufeira", listingCount: listingCount);

            stats.ProjectCount = projectCount;
            stats.ProjectP25PricePerM2 = projectP25;
            stats.ProjectMedianPricePerM2 = projectMedian;
            stats.ProjectP75PricePerM2 = projectP75;
            stats.MoveInCount = moveInCount;
            stats.MoveInP25PricePerM2 = moveInP25;
            stats.MoveInMedianPricePerM2 = moveInMedian;
            stats.MoveInP75PricePerM2 = moveInP75;

            return stats;
        }
    }
}
