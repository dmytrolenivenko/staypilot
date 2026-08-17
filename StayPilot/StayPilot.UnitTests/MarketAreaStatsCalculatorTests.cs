using StayPilot.Application.Helpers.Calculators;
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;

namespace StayPilot.UnitTests
{
    /// <summary>
    /// The calculator does two things worth testing: it rolls each listing up into three levels,
    /// and it takes a median rather than an average. Everything here checks one of those, plus
    /// the listings it is supposed to throw away.
    /// </summary>
    public class MarketAreaStatsCalculatorTests
    {
        [Fact]
        public void Calculate_OneListing_CountsIntoAllThreeLevels()
        {
            var listings = new List<PropertyListing>
            {
                Listing("Faro", "Albufeira", "Guia", 3000m)
            };

            var rows = MarketAreaStatsCalculator.Calculate(listings);

            // One listing, three rows: its town, its municipality, its district.
            Assert.Equal(3, rows.Count);
            Assert.Equal(3000m, Row(rows, AreaLevel.Town, "Faro", "Albufeira", "Guia").MedianPricePerM2);
            Assert.Equal(3000m, Row(rows, AreaLevel.Municipality, "Faro", "Albufeira").MedianPricePerM2);
            Assert.Equal(3000m, Row(rows, AreaLevel.District, "Faro").MedianPricePerM2);
        }

        [Fact]
        public void Calculate_DistrictRow_IsBuiltFromListingsNotFromTheMunicipalityRows()
        {
            // Albufeira has three listings, Loulé has one. A plain average of the two
            // municipality medians would read 3000; the district must follow the listings.
            var listings = new List<PropertyListing>
            {
                Listing("Faro", "Albufeira", "Guia", 2000m),
                Listing("Faro", "Albufeira", "Guia", 2000m),
                Listing("Faro", "Albufeira", "Guia", 2000m),
                Listing("Faro", "Loulé", "Quarteira", 9000m)
            };

            var rows = MarketAreaStatsCalculator.Calculate(listings);
            var district = Row(rows, AreaLevel.District, "Faro");

            Assert.Equal(4, district.ListingCount);
            Assert.Equal(2000m, district.MedianPricePerM2);
        }

        [Fact]
        public void Calculate_OneVeryExpensiveVilla_DoesNotDragTheMedianUp()
        {
            var listings = new List<PropertyListing>
            {
                Listing("Faro", "Albufeira", "Guia", 2000m),
                Listing("Faro", "Albufeira", "Guia", 2100m),
                Listing("Faro", "Albufeira", "Guia", 50000m)
            };

            var rows = MarketAreaStatsCalculator.Calculate(listings);

            // The average would be 18033. The middle value is what we want.
            Assert.Equal(2100m, Row(rows, AreaLevel.Town, "Faro", "Albufeira", "Guia").MedianPricePerM2);
        }

        [Fact]
        public void Calculate_EvenNumberOfListings_SplitsTheTwoMiddleValues()
        {
            var listings = new List<PropertyListing>
            {
                Listing("Faro", "Albufeira", "Guia", 1000m),
                Listing("Faro", "Albufeira", "Guia", 2000m),
                Listing("Faro", "Albufeira", "Guia", 3000m),
                Listing("Faro", "Albufeira", "Guia", 6000m)
            };

            var rows = MarketAreaStatsCalculator.Calculate(listings);

            Assert.Equal(2500m, Row(rows, AreaLevel.Town, "Faro", "Albufeira", "Guia").MedianPricePerM2);
        }

        [Fact]
        public void Calculate_SameTownNameInTwoDistricts_StaysApart()
        {
            // The Odivelas case: a town in Beja and another in Lisboa. Merging them would
            // average two places hundreds of kilometres apart into one number.
            var listings = new List<PropertyListing>
            {
                Listing("Beja", "Ferreira do Alentejo", "Odivelas", 800m),
                Listing("Lisboa", "Odivelas", "Odivelas", 3500m)
            };

            var rows = MarketAreaStatsCalculator.Calculate(listings);

            Assert.Equal(800m, Row(rows, AreaLevel.Town, "Beja", "Ferreira do Alentejo", "Odivelas").MedianPricePerM2);
            Assert.Equal(3500m, Row(rows, AreaLevel.Town, "Lisboa", "Odivelas", "Odivelas").MedianPricePerM2);
        }

        [Fact]
        public void Calculate_UsesTheNewestSnapshot()
        {
            var listing = Listing("Faro", "Albufeira", "Guia", 2000m);

            listing.ListingSnapshots.Add(new ListingSnapshot
            {
                PricePerM2 = 2500m,
                Price = 250000m,
                SnapshotDateUtc = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc)
            });

            var rows = MarketAreaStatsCalculator.Calculate(new List<PropertyListing> { listing });

            Assert.Equal(2500m, Row(rows, AreaLevel.Town, "Faro", "Albufeira", "Guia").MedianPricePerM2);
        }

        [Fact]
        public void Calculate_ListingsWithNoUsablePrice_AreIgnored()
        {
            var noSnapshot = Listing("Faro", "Albufeira", "Guia", 2000m);
            noSnapshot.ListingSnapshots.Clear();

            var listings = new List<PropertyListing>
            {
                Listing("Faro", "Albufeira", "Guia", 2000m),
                noSnapshot,
                Listing("Faro", "Albufeira", "Guia", 0m)
            };

            var rows = MarketAreaStatsCalculator.Calculate(listings);

            Assert.Equal(1, Row(rows, AreaLevel.Town, "Faro", "Albufeira", "Guia").ListingCount);
        }

        [Fact]
        public void Calculate_ListingWithNoMarketArea_IsIgnored()
        {
            var orphan = Listing("Faro", "Albufeira", "Guia", 2000m);
            orphan.MarketArea = null!;

            var rows = MarketAreaStatsCalculator.Calculate(new List<PropertyListing> { orphan });

            Assert.Empty(rows);
        }

        [Fact]
        public void Calculate_BlankTownName_StillCountsTowardsTheLevelsAboveIt()
        {
            var listings = new List<PropertyListing>
            {
                Listing("Faro", "Albufeira", string.Empty, 2000m)
            };

            var rows = MarketAreaStatsCalculator.Calculate(listings);

            // No town row can be written without a town name, but the listing is not lost.
            Assert.Equal(2, rows.Count);
            Assert.DoesNotContain(rows, x => x.Level == AreaLevel.Town);
            Assert.Equal(1, Row(rows, AreaLevel.Municipality, "Faro", "Albufeira").ListingCount);
        }

        [Fact]
        public void Calculate_NoListings_ReturnsNoRows()
        {
            var rows = MarketAreaStatsCalculator.Calculate(new List<PropertyListing>());

            Assert.Empty(rows);
        }

        /// <summary>
        /// One listing in one place, with a single price. Enough for every test here: the
        /// calculator only ever reads the market area and the newest price.
        /// </summary>
        private static PropertyListing Listing(string district, string municipality, string town, decimal pricePerM2)
        {
            return new PropertyListing
            {
                MarketArea = new MarketArea
                {
                    District = district,
                    Municipality = municipality,
                    Town = town
                },
                ListingSnapshots = new List<ListingSnapshot>
                {
                    new ListingSnapshot
                    {
                        PricePerM2 = pricePerM2,
                        Price = pricePerM2 * 100,
                        SnapshotDateUtc = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc)
                    }
                }
            };
        }

        /// <summary>Finds the one row for a place, and fails the test when it is missing.</summary>
        private static MarketAreaStats Row(List<MarketAreaStats> rows, AreaLevel level, string district, string municipality = "", string town = "")
        {
            return Assert.Single(rows, x =>
                x.Level == level &&
                x.District == district &&
                x.Municipality == municipality &&
                x.Town == town);
        }
    }
}
