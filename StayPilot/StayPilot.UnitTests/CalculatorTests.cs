using StayPilot.Application.Helpers.Calculators;
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;

namespace StayPilot.UnitTests
{
    /// <summary>
    /// Tests for the shared maths/text helpers in <see cref="Calculator"/>.
    /// Real Algarve coordinates are used so the numbers can be checked against a map.
    /// </summary>
    public class CalculatorTests
    {
        // Central Quarteira, used as the reference point in the distance tests.
        private const double QuarteiraLat = 37.0690;
        private const double QuarteiraLon = -8.1030;

        [Fact]
        public void CalculateDistanceMeters_SamePoint_IsZero()
        {
            var meters = Calculator.CalculateDistanceMeters(QuarteiraLat, QuarteiraLon, QuarteiraLat, QuarteiraLon);

            Assert.Equal(0, meters, 6);
        }

        [Fact]
        public void CalculateDistanceMeters_OneDegreeOfLatitude_IsAboutOneHundredAndElevenKm()
        {
            // A degree of latitude is the same length anywhere on Earth: ~111.2 km.
            var meters = Calculator.CalculateDistanceMeters(37.0, -8.0, 38.0, -8.0);

            Assert.InRange(meters, 111_000, 111_400);
        }

        [Fact]
        public void CalculateDistanceMeters_OneDegreeOfLongitude_IsShorterAwayFromTheEquator()
        {
            // At 37N a degree of longitude is only ~88.9 km, not ~111 km. This is the whole
            // reason the repository scales longitude by cos(latitude).
            var meters = Calculator.CalculateDistanceMeters(37.0, -8.0, 37.0, -7.0);

            Assert.InRange(meters, 88_500, 89_300);
        }

        [Fact]
        public void CalculateDistanceMeters_QuarteiraToVilamoura_IsAboutSixKm()
        {
            // Marina de Vilamoura is roughly 6 km west of central Quarteira.
            var meters = Calculator.CalculateDistanceMeters(QuarteiraLat, QuarteiraLon, 37.0745, -8.1250);

            Assert.InRange(meters, 1_500, 3_000);
        }

        [Fact]
        public void CalculateDistanceMeters_IsSymmetric()
        {
            var there = Calculator.CalculateDistanceMeters(QuarteiraLat, QuarteiraLon, 37.1000, -8.2000);
            var back = Calculator.CalculateDistanceMeters(37.1000, -8.2000, QuarteiraLat, QuarteiraLon);

            Assert.Equal(there, back, 6);
        }

        [Theory]
        [InlineData("  Faró ", "faro")]
        [InlineData("Loulé", "loule")]
        [InlineData("ALBUFEIRA", "albufeira")]
        [InlineData("", "")]
        [InlineData("   ", "")]
        public void NormalizeText_StripsAccentsCaseAndSpaces(string input, string expected)
        {
            Assert.Equal(expected, Calculator.NormalizeText(input));
        }

        [Fact]
        public void GetTheClosestBeach_NoCoordinates_ReturnsNull()
        {
            var beaches = new List<BeachMarker>
            {
                new() { Id = 1, Latitude = 37.06m, Longitude = -8.10m }
            };

            Assert.Null(Calculator.GetTheClosestBeach(beaches, null, -8.10m));
            Assert.Null(Calculator.GetTheClosestBeach(beaches, 37.06m, null));
        }

        [Fact]
        public void GetTheClosestBeach_PicksTheNearestOne()
        {
            var nearBeach = new BeachMarker { Id = 1, Latitude = 37.0700m, Longitude = -8.1035m };
            var farBeach = new BeachMarker { Id = 2, Latitude = 37.5000m, Longitude = -8.9000m };

            // Far one listed first, so this fails if the method just takes the head of the list.
            var beaches = new List<BeachMarker> { farBeach, nearBeach };

            var closest = Calculator.GetTheClosestBeach(beaches, (decimal)QuarteiraLat, (decimal)QuarteiraLon);

            Assert.NotNull(closest);
            Assert.Equal(nearBeach.Id, closest!.Id);
        }

        [Fact]
        public void GetMarketId_ExactMatchOnEveryPart_Wins()
        {
            var areas = new List<MarketArea>
            {
                new() { Id = 10, Country = "Portugal", District = "Faro", Municipality = "Loulé", Town = "Quarteira", Zone = "Vilamoura" },
                new() { Id = 11, Country = "Portugal", District = "Faro", Municipality = "Loulé", Town = "Quarteira", Zone = "Praia de Quarteira" }
            };

            var id = Calculator.GetMarketId(areas, "Portugal", "Faro", "Loulé", "Quarteira", "Praia de Quarteira");

            Assert.Equal(11, id);
        }

        [Fact]
        public void GetMarketId_UnknownZone_FallsBackToTheTown()
        {
            var areas = new List<MarketArea>
            {
                new() { Id = 10, Country = "Portugal", District = "Faro", Municipality = "Loulé", Town = "Quarteira", Zone = "Vilamoura" },
                new() { Id = 11, Country = "Portugal", District = "Faro", Municipality = "Loulé", Town = "Quarteira", Zone = null }
            };

            // The zone does not exist, so it should land on the town - preferring the row with no zone.
            var id = Calculator.GetMarketId(areas, "Portugal", "Faro", "Loulé", "Quarteira", "Zone That Does Not Exist");

            Assert.Equal(11, id);
        }

        [Fact]
        public void GetMarketId_AccentsAndCasingDoNotMatter()
        {
            var areas = new List<MarketArea>
            {
                new() { Id = 10, Country = "Portugal", District = "Faro", Municipality = "Loulé", Town = "Quarteira", Zone = "Vilamoura" }
            };

            var id = Calculator.GetMarketId(areas, "PORTUGAL", "faro", "Loule", "quarteira", "VILAMOURA");

            Assert.Equal(10, id);
        }

        [Fact]
        public void GetMarketId_NothingMatches_Throws()
        {
            var areas = new List<MarketArea>
            {
                new() { Id = 10, Country = "Portugal", District = "Faro", Municipality = "Loulé", Town = "Quarteira", Zone = "Vilamoura" }
            };

            Assert.Throws<InvalidOperationException>(
                () => Calculator.GetMarketId(areas, "Spain", "Madrid", "Madrid", "Madrid"));
        }

        [Fact]
        public void CalculateFeaturePremiumPercent_NotEnoughData_ReturnsZero()
        {
            // Two listings is well under MinimumListingsPerGroup, so there is nothing to trust.
            var listings = new List<PropertyListing>
            {
                MakeListing(1, hasGarage: true, pricePerM2: 5000),
                MakeListing(2, hasGarage: false, pricePerM2: 4000)
            };

            var premium = Calculator.CalculateFeaturePremiumPercent(listings, PremiumFeatures.HasGarage);

            Assert.Equal(0, premium);
        }

        [Fact]
        public void CalculateFeaturePremiumPercent_GarageWorthMore_ReturnsAPositivePercent()
        {
            var listings = new List<PropertyListing>();

            // Ten listings in one bucket: five with a garage at 5500, five without at 5000.
            // That is a 10% premium, and both sides clear the minimum of five.
            for (var i = 0; i < 5; i++)
            {
                listings.Add(MakeListing(i + 1, hasGarage: true, pricePerM2: 5500));
                listings.Add(MakeListing(i + 100, hasGarage: false, pricePerM2: 5000));
            }

            var premium = Calculator.CalculateFeaturePremiumPercent(listings, PremiumFeatures.HasGarage);

            Assert.InRange(premium, 9.5m, 10.5m);
        }

        [Fact]
        public void CalculateFeaturePremiumPercent_ListingsOutsideTheWhitelistedTowns_AreIgnored()
        {
            var listings = new List<PropertyListing>();

            // Both Town AND Municipality have to be off the whitelist - the filter accepts
            // a listing if either one matches, because some sources fill only one of them.
            for (var i = 0; i < 5; i++)
            {
                listings.Add(MakeListing(i + 1, hasGarage: true, pricePerM2: 5500, town: "Braga", municipality: "Braga"));
                listings.Add(MakeListing(i + 100, hasGarage: false, pricePerM2: 5000, town: "Braga", municipality: "Braga"));
            }

            var premium = Calculator.CalculateFeaturePremiumPercent(listings, PremiumFeatures.HasGarage);

            Assert.Equal(0, premium);
        }

        /// <summary>
        /// Builds one listing with a single snapshot, in a whitelisted town by default,
        /// so the premium calculator will actually look at it.
        /// </summary>
        private static PropertyListing MakeListing(int id, bool hasGarage, decimal pricePerM2, string town = "Quarteira", string municipality = "Loulé")
        {
            return new PropertyListing
            {
                Id = id,
                SourceUrl = $"https://example.test/{id}",
                PropertyType = PropertyType.Apartment,
                Typology = Typology.T1,
                AreaM2 = 60,
                MarketAreaId = 1,
                MarketArea = new MarketArea
                {
                    Id = 1,
                    Country = "Portugal",
                    District = "Faro",
                    Municipality = municipality,
                    Town = town,
                    Zone = "Centro"
                },
                HasGarage = hasGarage,
                ListingSnapshots = new List<ListingSnapshot>
                {
                    new()
                    {
                        Id = id,
                        PropertyListingId = id,
                        Price = pricePerM2 * 60,
                        PricePerM2 = pricePerM2,
                        SnapshotDateUtc = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc)
                    }
                }
            };
        }
    }
}
