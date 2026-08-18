using StayPilot.Application.Helpers.Calculators;
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;

namespace StayPilot.UnitTests
{
    // The demand score, on the two things it is allowed to look at: how long homes sit, and
    // whether new adverts are arriving faster than they were.
    //
    // The point most of these defend is the difference between "average" and "not measured".
    // The scale has a middle, and an unmeasured place must never land in it.
    public class DemandCalculatorTests
    {
        private static readonly DateTime Now = new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc);

        // One listing: first seen firstSeenDaysAgo, last snapshot lastSeenDaysAgo, in that state.
        private static PropertyListing Listing(int firstSeenDaysAgo, int lastSeenDaysAgo, ListingStatus status = ListingStatus.Active, decimal pricePerM2 = 3000m)
        {
            return new PropertyListing
            {
                CreatedAtUtc = Now.AddDays(-firstSeenDaysAgo),
                ListingSnapshots = new List<ListingSnapshot>
                {
                    new ListingSnapshot
                    {
                        SnapshotDateUtc = Now.AddDays(-lastSeenDaysAgo),
                        Price = pricePerM2 * 80m,
                        PricePerM2 = pricePerM2,
                        Status = status,
                    },
                },
            };
        }

        // A place with a long enough history for both halves to be allowed to speak.
        // "olderCount" listings arrived in the earlier 90 day window, "recentCount" in the later.
        private static List<PropertyListing> Place(int recentCount, int previousCount, int ageOfLiveListingsDays, int historyDays = 400)
        {
            var listings = new List<PropertyListing>
            {
                // Anchors the collection window so the guards have a history to measure against.
                Listing(historyDays, ageOfLiveListingsDays),
            };

            for (var i = 0; i < recentCount; i++)
            {
                listings.Add(Listing(10 + i, 0));
            }

            for (var i = 0; i < previousCount; i++)
            {
                listings.Add(Listing(120 + i, 0));
            }

            return listings;
        }

        [Fact]
        public void Calculate_TooFewListings_ReportsNotMeasured()
        {
            var listings = Enumerable.Range(0, DemandCalculator.MinimumListings - 1)
                .Select(i => Listing(200 + i, 0))
                .ToList();

            var outcome = DemandCalculator.Calculate(listings, Now);

            Assert.False(outcome.IsMeasurable);
            Assert.Equal(0m, outcome.Score);
            Assert.Contains("at least", outcome.Reason);
        }

        [Fact]
        public void Calculate_ListingsWithNoSnapshots_AreNotCounted()
        {
            var listings = Enumerable.Range(0, 20)
                .Select(_ => new PropertyListing { CreatedAtUtc = Now.AddDays(-100) })
                .ToList();

            var outcome = DemandCalculator.Calculate(listings, Now);

            // A listing we have never priced tells us nothing about how long homes sit.
            Assert.False(outcome.IsMeasurable);
            Assert.Equal(0, outcome.SampleSize);
        }

        // The trap this whole guard exists for: early on, every listing looks young because
        // collection is young. Without it a brand new database reads as the hottest market alive.
        [Fact]
        public void Calculate_MedianAgeFillsTheCollectionWindow_DoesNotScoreDaysOnMarket()
        {
            // Nine days of history, and homes "have been up" nine days. Both are the same fact.
            var listings = Enumerable.Range(0, 20)
                .Select(i => Listing(9 - (i % 3), 0))
                .ToList();

            var outcome = DemandCalculator.Calculate(listings, Now);

            Assert.Null(outcome.DaysScore);
            Assert.NotNull(outcome.MedianDaysOnMarket);
            Assert.Contains("too close to the", outcome.Reason);
        }

        [Fact]
        public void Calculate_ShortHistory_DoesNotCompareSupply()
        {
            var listings = Enumerable.Range(0, 20).Select(i => Listing(50 + i, 0)).ToList();

            var outcome = DemandCalculator.Calculate(listings, Now);

            // 69 days of history cannot hold two 90 day windows.
            Assert.Null(outcome.SupplyScore);
            Assert.Contains("comparing supply needs", outcome.Reason);
        }

        [Fact]
        public void Calculate_NeitherHalfMeasurable_IsNotBalanced()
        {
            // Young collection: days-on-market is saturated and supply has no earlier window.
            var listings = Enumerable.Range(0, 20).Select(i => Listing(5 + (i % 3), 0)).ToList();

            var outcome = DemandCalculator.Calculate(listings, Now);

            Assert.False(outcome.IsMeasurable);
            Assert.Equal(0m, outcome.Score);
        }

        [Fact]
        public void Calculate_HomesSellFast_ScoresHigh()
        {
            // Eight sold listings that went in under a month, against a long collection window.
            var listings = new List<PropertyListing> { Listing(400, 380, ListingStatus.Active) };

            for (var i = 0; i < DemandCalculator.MinimumSoldListings + 4; i++)
            {
                listings.Add(Listing(220 + i, 200 + i, ListingStatus.Sold));
            }

            var outcome = DemandCalculator.Calculate(listings, Now);

            Assert.True(outcome.DaysMeasuredOnSold);
            Assert.Equal(20m, outcome.MedianDaysOnMarket);
            Assert.Equal(100m, outcome.DaysScore);
            Assert.Contains("homes that sold sat", outcome.Reason);
        }

        [Fact]
        public void Calculate_HomesSitForHalfAYear_ScoresZeroOnDays()
        {
            var listings = new List<PropertyListing> { Listing(900, 880, ListingStatus.Active) };

            for (var i = 0; i < DemandCalculator.MinimumSoldListings + 4; i++)
            {
                // First seen 600 days ago, sold 400 days later.
                listings.Add(Listing(600, 200, ListingStatus.Sold));
            }

            var outcome = DemandCalculator.Calculate(listings, Now);

            Assert.Equal(0m, outcome.DaysScore);
        }

        // Sold listings answer "how long does it take" properly; live ones only put a floor on it.
        // Below the threshold the live ones are used, and the reason says which was used.
        [Fact]
        public void Calculate_TooFewSoldListings_FallsBackToLiveOnes()
        {
            var listings = new List<PropertyListing> { Listing(400, 0) };

            for (var i = 0; i < DemandCalculator.MinimumSoldListings - 1; i++)
            {
                listings.Add(Listing(220, 210, ListingStatus.Sold));
            }

            for (var i = 0; i < 15; i++)
            {
                listings.Add(Listing(40 + i, 0));
            }

            var outcome = DemandCalculator.Calculate(listings, Now);

            Assert.False(outcome.DaysMeasuredOnSold);
            Assert.Contains("homes still up have been up", outcome.Reason);
        }

        [Fact]
        public void Calculate_NewSupplyPilingUp_ScoresLowOnSupply()
        {
            // Half as much stock again arriving as in the window before: the glut end of the scale.
            var outcome = DemandCalculator.Calculate(Place(recentCount: 30, previousCount: 20, ageOfLiveListingsDays: 0), Now);

            Assert.Equal(50m, outcome.SupplyChangePercent);
            Assert.Equal(0m, outcome.SupplyScore);
            Assert.Contains("more new adverts", outcome.Reason);
        }

        [Fact]
        public void Calculate_NewSupplyDryingUp_ScoresHighOnSupply()
        {
            var outcome = DemandCalculator.Calculate(Place(recentCount: 10, previousCount: 20, ageOfLiveListingsDays: 0), Now);

            Assert.Equal(-50m, outcome.SupplyChangePercent);
            Assert.Equal(100m, outcome.SupplyScore);
            Assert.Contains("fewer new adverts", outcome.Reason);
        }

        [Fact]
        public void Calculate_SupplySteady_ScoresTheMiddle()
        {
            var outcome = DemandCalculator.Calculate(Place(recentCount: 20, previousCount: 20, ageOfLiveListingsDays: 0), Now);

            Assert.Equal(0m, outcome.SupplyChangePercent);
            Assert.Equal(50m, outcome.SupplyScore);
            Assert.Contains("about the same rate", outcome.Reason);
        }

        [Fact]
        public void Calculate_NoEarlierWindow_DoesNotInventASupplyChange()
        {
            var outcome = DemandCalculator.Calculate(Place(recentCount: 25, previousCount: 0, ageOfLiveListingsDays: 0), Now);

            Assert.Null(outcome.SupplyChangePercent);
            Assert.Null(outcome.SupplyScore);
            Assert.Contains("no listings in the earlier window", outcome.Reason);
        }

        [Fact]
        public void Calculate_OnlyOneHalfMeasurable_SaysSoAndScoresOnIt()
        {
            // Long history, so how long homes sit is measurable - but nothing arrived in the
            // earlier window, so there is no before to compare new supply against.
            var outcome = DemandCalculator.Calculate(Place(recentCount: 25, previousCount: 0, ageOfLiveListingsDays: 0), Now);

            Assert.True(outcome.IsMeasurable);
            Assert.NotNull(outcome.DaysScore);
            Assert.Null(outcome.SupplyScore);
            Assert.Equal(outcome.DaysScore, outcome.Score);
            Assert.Contains("scored on the other half alone", outcome.Reason);
        }

        [Theory]
        [InlineData(0, DemandLevel.Cold)]
        [InlineData(19.9, DemandLevel.Cold)]
        [InlineData(20, DemandLevel.Soft)]
        [InlineData(40, DemandLevel.Balanced)]
        [InlineData(60, DemandLevel.Firm)]
        [InlineData(80, DemandLevel.Hot)]
        [InlineData(100, DemandLevel.Hot)]
        public void Band_PutsTheScoreInTheRightWord(double score, DemandLevel expected)
        {
            Assert.Equal(expected, DemandCalculator.Band((decimal)score));
        }
    }
}
