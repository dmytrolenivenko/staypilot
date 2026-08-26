using StayPilot.Application.Helpers.Calculators;
using StayPilot.Application.Interfaces.Repositories;
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
            var listings = new List<MarketAreaStatsListingRow>
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
            var listings = new List<MarketAreaStatsListingRow>
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
            var listings = new List<MarketAreaStatsListingRow>
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
            var listings = new List<MarketAreaStatsListingRow>
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
            var listings = new List<MarketAreaStatsListingRow>
            {
                Listing("Beja", "Ferreira do Alentejo", "Odivelas", 800m),
                Listing("Lisboa", "Odivelas", "Odivelas", 3500m)
            };

            var rows = MarketAreaStatsCalculator.Calculate(listings);

            Assert.Equal(800m, Row(rows, AreaLevel.Town, "Beja", "Ferreira do Alentejo", "Odivelas").MedianPricePerM2);
            Assert.Equal(3500m, Row(rows, AreaLevel.Town, "Lisboa", "Odivelas", "Odivelas").MedianPricePerM2);
        }

        [Fact]
        public void Calculate_ListingsWithNoUsablePrice_AreIgnored()
        {
            var listings = new List<MarketAreaStatsListingRow>
            {
                Listing("Faro", "Albufeira", "Guia", 2000m),
                Listing("Faro", "Albufeira", "Guia", 0m) // no price - the repository sends the same
                                                          // zero when a listing has no snapshot at all
            };

            var rows = MarketAreaStatsCalculator.Calculate(listings);

            Assert.Equal(1, Row(rows, AreaLevel.Town, "Faro", "Albufeira", "Guia").ListingCount);
        }

        [Fact]
        public void Calculate_ListingWithNoPlaceRecorded_IsIgnoredEntirely()
        {
            var orphan = Listing(string.Empty, string.Empty, string.Empty, 2000m);

            var rows = MarketAreaStatsCalculator.Calculate(new List<MarketAreaStatsListingRow> { orphan });

            Assert.Empty(rows);
        }

        [Fact]
        public void Calculate_BlankTownName_StillCountsTowardsTheLevelsAboveIt()
        {
            var listings = new List<MarketAreaStatsListingRow>
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
            var rows = MarketAreaStatsCalculator.Calculate(new List<MarketAreaStatsListingRow>());

            Assert.Empty(rows);
        }

        [Fact]
        public void Calculate_ListingWithNoArea_IsIgnored()
        {
            var listings = new List<MarketAreaStatsListingRow>
            {
                Listing("Faro", "Albufeira", "Guia", 2000m, areaM2: 0)
            };

            var rows = MarketAreaStatsCalculator.Calculate(listings);

            Assert.Empty(rows);
        }

        [Fact]
        public void Calculate_MedianAreaM2_IsTheMiddleArea()
        {
            var listings = new List<MarketAreaStatsListingRow>
            {
                Listing("Faro", "Albufeira", "Guia", 2000m, areaM2: 50),
                Listing("Faro", "Albufeira", "Guia", 2000m, areaM2: 80),
                Listing("Faro", "Albufeira", "Guia", 2000m, areaM2: 300)
            };

            var rows = MarketAreaStatsCalculator.Calculate(listings);

            Assert.Equal(80m, Row(rows, AreaLevel.Town, "Faro", "Albufeira", "Guia").MedianAreaM2);
        }

        // --- Typology rows: what a budget screen reads ---------------------------------

        [Fact]
        public void Calculate_TypologyWithFewerThanThreeListings_GetsNoRow()
        {
            var listings = new List<MarketAreaStatsListingRow>
            {
                Listing("Faro", "Albufeira", "Guia", 2000m, typology: Typology.T1),
                Listing("Faro", "Albufeira", "Guia", 2000m, typology: Typology.T1)
            };

            var rows = MarketAreaStatsCalculator.Calculate(listings);

            // Two adverts is not a "median T1 price" - it is two adverts.
            Assert.Empty(Row(rows, AreaLevel.Town, "Faro", "Albufeira", "Guia").TypologyStats);
        }

        [Fact]
        public void Calculate_TypologyWithEnoughListings_GetsItsOwnPriceAndArea()
        {
            var listings = ThreeListings(3000m, areaM2: 90, typology: Typology.T2);

            var rows = MarketAreaStatsCalculator.Calculate(listings);
            var typology = Assert.Single(Row(rows, AreaLevel.Town, "Faro", "Albufeira", "Guia").TypologyStats);

            Assert.Equal(Typology.T2, typology.Typology);
            Assert.Equal(3, typology.ListingCount);
            Assert.Equal(270000m, typology.MedianPrice); // 3000 x 90
            Assert.Equal(90m, typology.MedianAreaM2);
            Assert.Equal(3000m, typology.MedianPricePerM2);
        }

        [Fact]
        public void Calculate_TypologiesAreKeptApart()
        {
            var listings = ThreeListings(2000m, areaM2: 60, typology: Typology.T1);
            listings.AddRange(ThreeListings(2000m, areaM2: 140, typology: Typology.T3));

            var rows = MarketAreaStatsCalculator.Calculate(listings);
            var typologies = Row(rows, AreaLevel.Town, "Faro", "Albufeira", "Guia").TypologyStats;

            Assert.Equal(2, typologies.Count);
            Assert.Equal(120000m, typologies.Single(x => x.Typology == Typology.T1).MedianPrice);
            Assert.Equal(280000m, typologies.Single(x => x.Typology == Typology.T3).MedianPrice);
        }

        // --- Renovation split ---------------------------------------------------------

        [Fact]
        public void Calculate_NeedsRenovation_CountsAsProject()
        {
            var listings = ThreeListings(1500m, condition: PropertyCondition.NeedsRenovation);

            var rows = MarketAreaStatsCalculator.Calculate(listings);
            var row = Row(rows, AreaLevel.Town, "Faro", "Albufeira", "Guia");

            Assert.Equal(3, row.ProjectCount);
            Assert.Equal(1500m, row.ProjectMedianPricePerM2);
            Assert.Equal(0, row.MoveInCount);
        }

        [Theory]
        [InlineData("D")]
        [InlineData("E")]
        [InlineData("F")]
        [InlineData("G")]
        public void Calculate_PoorEnergyGrade_CountsAsProjectEvenInGoodCondition(string grade)
        {
            // The grade is the objective signal. "Needs renovation" sits on 1.4% of real stock,
            // far too few to measure a discount from, so a poor certificate carries this.
            var listings = ThreeListings(1500m, condition: PropertyCondition.Good, energyCertificate: grade);

            var rows = MarketAreaStatsCalculator.Calculate(listings);

            Assert.Equal(3, Row(rows, AreaLevel.Town, "Faro", "Albufeira", "Guia").ProjectCount);
        }

        [Theory]
        [InlineData("A+")]
        [InlineData("A")]
        [InlineData("B-")]
        [InlineData("C")]
        public void Calculate_GoodEnergyGrade_CountsAsMoveInReady(string grade)
        {
            // "A+" and "B-" have to read as A and B, not as unknown.
            var listings = ThreeListings(4000m, condition: PropertyCondition.Good, energyCertificate: grade);

            var rows = MarketAreaStatsCalculator.Calculate(listings);
            var row = Row(rows, AreaLevel.Town, "Faro", "Albufeira", "Guia");

            Assert.Equal(3, row.MoveInCount);
            Assert.Equal(0, row.ProjectCount);
            Assert.Equal(4000m, row.MoveInMedianPricePerM2);
        }

        [Fact]
        public void Calculate_UnknownConditionAndNoGrade_CountsAsNeither()
        {
            // Neither group should absorb a listing we know nothing about, or the discount would
            // be measured against guesses.
            var listings = ThreeListings(2000m, condition: PropertyCondition.Unknown, energyCertificate: null);

            var rows = MarketAreaStatsCalculator.Calculate(listings);
            var row = Row(rows, AreaLevel.Town, "Faro", "Albufeira", "Guia");

            Assert.Equal(0, row.ProjectCount);
            Assert.Equal(0, row.MoveInCount);
            Assert.Equal(3, row.ListingCount);
        }

        [Fact]
        public void Calculate_TooFewProjects_LeavesTheProjectMedianNull()
        {
            var listings = ThreeListings(4000m, condition: PropertyCondition.Good);
            listings.Add(Listing("Faro", "Albufeira", "Guia", 1000m, condition: PropertyCondition.NeedsRenovation));

            var rows = MarketAreaStatsCalculator.Calculate(listings);
            var row = Row(rows, AreaLevel.Town, "Faro", "Albufeira", "Guia");

            // One project is counted but not turned into a median, so nothing downstream can
            // report a renovation discount off a single advert.
            Assert.Equal(1, row.ProjectCount);
            Assert.Null(row.ProjectMedianPricePerM2);
            Assert.Equal(4000m, row.MoveInMedianPricePerM2);
        }

        // --- Centroids: what the neighbour screen needs -------------------------------

        [Fact]
        public void Calculate_Centroid_IsTheMiddleOfTheListings()
        {
            var listings = new List<MarketAreaStatsListingRow>
            {
                Listing("Faro", "Albufeira", "Guia", 2000m, latitude: 37.0m, longitude: -8.0m),
                Listing("Faro", "Albufeira", "Guia", 2000m, latitude: 37.2m, longitude: -8.4m),
            };

            var rows = MarketAreaStatsCalculator.Calculate(listings);
            var row = Row(rows, AreaLevel.Town, "Faro", "Albufeira", "Guia");

            Assert.Equal(37.1m, row.CentroidLatitude);
            Assert.Equal(-8.2m, row.CentroidLongitude);
        }

        // --- Deals --------------------------------------------------------------------

        [Fact]
        public void Calculate_AnyListings_CountsNoDeals()
        {
            // No pricing model backs a "below estimate" flag any more, so the count stays at
            // zero rather than falling back to something like "cheaper than its neighbours".
            var rows = MarketAreaStatsCalculator.Calculate(ThreeListings(2000m));

            Assert.Equal(0, Row(rows, AreaLevel.Town, "Faro", "Albufeira", "Guia").BelowEstimateCount);
        }

        /// <summary>
        /// One listing in one place. The area defaults to 100m² so Price stays PricePerM2 × 100
        /// and the two never disagree. Everything past the price is optional: pass only what the
        /// test is actually about.
        /// </summary>
        private static MarketAreaStatsListingRow Listing(
            string district,
            string municipality,
            string town,
            decimal pricePerM2,
            int areaM2 = 100,
            Typology typology = Typology.T2,
            PropertyCondition condition = PropertyCondition.Good,
            string? energyCertificate = "B",
            decimal latitude = 37.08m,
            decimal longitude = -8.10m)
        {
            return new MarketAreaStatsListingRow(
                pricePerM2 * areaM2, pricePerM2, areaM2, typology, condition, energyCertificate,
                latitude, longitude, district, municipality, town);
        }

        /// <summary>Three alike listings, the fewest a typology or a split row needs.</summary>
        private static List<MarketAreaStatsListingRow> ThreeListings(
            decimal pricePerM2,
            int areaM2 = 100,
            Typology typology = Typology.T2,
            PropertyCondition condition = PropertyCondition.Good,
            string? energyCertificate = "B")
        {
            return Enumerable.Range(0, 3)
                .Select(_ => Listing("Faro", "Albufeira", "Guia", pricePerM2, areaM2, typology, condition, energyCertificate))
                .ToList();
        }

        [Fact]
        public void Calculate_ProjectStock_SplitsWhyEachOneWasFlaggedAndCountsWhatItCouldNotJudge()
        {
            var listings = new List<MarketAreaStatsListingRow>();

            // Three the advert itself calls out, three caught only by a poor energy grade.
            listings.AddRange(ThreeListings(1_500m, condition: PropertyCondition.NeedsRenovation, energyCertificate: "B"));
            listings.AddRange(ThreeListings(1_600m, condition: PropertyCondition.Good, energyCertificate: "E"));

            // Three clearly finished, and three we cannot judge at all.
            listings.AddRange(ThreeListings(2_500m, condition: PropertyCondition.Renovated, energyCertificate: "A"));
            listings.AddRange(ThreeListings(2_400m, condition: PropertyCondition.Used, energyCertificate: null));

            var row = Row(MarketAreaStatsCalculator.Calculate(listings), AreaLevel.Town, "Faro", "Albufeira", "Guia");

            Assert.Equal(6, row.ProjectCount);

            // The advert's own word wins when both apply, so the two never double-count.
            Assert.Equal(3, row.ProjectByConditionCount);
            Assert.Equal(3, row.ProjectByEnergyCount);
            Assert.Equal(3, row.MoveInCount);

            // The whole point of this count: the discount rests on 9 of the 12 listings here, and
            // a reader who cannot see the other 3 will trust it more than it deserves.
            Assert.Equal(3, row.UnclassifiedCount);
        }

        [Fact]
        public void Calculate_ProjectStock_CarriesTheSpreadAndTheSizeItWasMeasuredOn()
        {
            var listings = new List<MarketAreaStatsListingRow>();

            listings.AddRange(ThreeListings(1_000m, areaM2: 80, condition: PropertyCondition.NeedsRenovation));
            listings.AddRange(ThreeListings(2_000m, areaM2: 80, condition: PropertyCondition.NeedsRenovation));
            listings.AddRange(ThreeListings(3_000m, areaM2: 120, condition: PropertyCondition.Renovated, energyCertificate: "A"));

            var row = Row(MarketAreaStatsCalculator.Calculate(listings), AreaLevel.Town, "Faro", "Albufeira", "Guia");

            // Six projects at 1,000 and 2,000: the middle half runs from 1,000 to 2,000.
            Assert.Equal(1_000m, row.ProjectP25PricePerM2);
            Assert.Equal(2_000m, row.ProjectP75PricePerM2);

            // Without the sizes the discount is a rate with nothing to multiply it by.
            Assert.Equal(80m, row.ProjectMedianAreaM2);
            Assert.Equal(120m, row.MoveInMedianAreaM2);
        }

        [Fact]
        public void Calculate_TooFewProjects_LeavesTheSpreadUnmeasuredRatherThanGuessing()
        {
            var listings = new List<MarketAreaStatsListingRow>
            {
                Listing("Faro", "Albufeira", "Guia", 1_500m, condition: PropertyCondition.NeedsRenovation)
            };

            listings.AddRange(ThreeListings(2_500m, condition: PropertyCondition.Renovated, energyCertificate: "A"));

            var row = Row(MarketAreaStatsCalculator.Calculate(listings), AreaLevel.Town, "Faro", "Albufeira", "Guia");

            // One project is counted, but nothing is measured off it - a quartile from a single
            // advert is that advert twice, and it would read on screen as a spread.
            Assert.Equal(1, row.ProjectCount);
            Assert.Null(row.ProjectMedianPricePerM2);
            Assert.Null(row.ProjectP25PricePerM2);
            Assert.Null(row.ProjectP75PricePerM2);
            Assert.Null(row.ProjectMedianAreaM2);
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
