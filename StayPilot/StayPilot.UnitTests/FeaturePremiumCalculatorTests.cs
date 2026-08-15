using Microsoft.EntityFrameworkCore;
using StayPilot.Application.Helpers.Calculators;
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;
using StayPilot.Infrastructure.Persistence;
using Xunit.Abstractions;

namespace StayPilot.UnitTests
{
    /// <summary>
    /// Every market here is built with the answer known by construction - a garage really is
    /// worth exactly 10%, and the confounders are planted deliberately - then the calculator has
    /// to recover it. That is the only way to test a statistical method: against real listings
    /// there is nothing to compare the output to.
    ///
    /// Several of these tests plant a confounder that the old hedonic regression got wrong, and
    /// assert the matched comparison does not. Those are the reason this class exists.
    ///
    /// The last member is not a test at all but an opt-in report against the real database - see
    /// <see cref="MatchedPremiums_AgainstTheRealListings_ReadNextToTheOldRegression"/>.
    /// </summary>
    public class FeaturePremiumCalculatorTests
    {
        private const decimal BasePricePerM2 = 5000m;

        private const string ConnectionString =
            "Server=(localdb)\\MSSQLLocalDB;Database=StayPilotCompsDb;Trusted_Connection=True;TrustServerCertificate=True;";

        private readonly ITestOutputHelper _output;

        public FeaturePremiumCalculatorTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Fit_TooFewListings_Throws()
        {
            var listings = BuildMarket(50, new MarketRules { GarageWorth = 1.10 });

            var error = Assert.Throws<InvalidOperationException>(() => FeaturePremiumCalculator.Fit(listings));

            Assert.Contains("at least", error.Message);
        }

        [Fact]
        public void Fit_BrokenListings_AreDroppedRatherThanMeasured()
        {
            var listings = BuildMarket(600, new MarketRules { GarageWorth = 1.10 });

            // The scraper's classic miss: a real price against a 2m² floor area. Left in, it
            // lands in the smallest size band and takes a whole stratum with it.
            listings.Add(NewListing(9001, areaM2: 2, pricePerM2: 174_500m));
            listings.Add(NewListing(9002, areaM2: 0, pricePerM2: 5000m));

            var calculator = FeaturePremiumCalculator.Fit(listings);

            Assert.Equal(600, calculator.TrainingListings);
        }

        [Fact]
        public void FeatureEffects_GarageWorthTenPercent_ReportsRoughlyTenPercent()
        {
            var listings = BuildMarket(600, new MarketRules { GarageWorth = 1.10 });

            var garage = Measure(listings, PremiumFeatures.HasGarage);

            Assert.InRange(garage.Percent, 8m, 12m);
            Assert.True(garage.IsMeasurable, "a clean 10% effect must come back as measurable");
        }

        [Fact]
        public void FeatureEffects_FeatureThatChangesNothing_IsReportedAsNotMeasurable()
        {
            // A garage that moves the price not at all, on one listing in twelve, against real
            // noise. There is barely a comparison to be had and the truth is zero, so the honest
            // answer is "cannot tell" - not a percentage with a confident range on it.
            var listings = BuildMarket(600, new MarketRules { GarageWorth = 1.0, Prevalence = 0.08, NoiseScale = 0.3 });

            var garage = Measure(listings, PremiumFeatures.HasGarage);

            Assert.False(garage.IsMeasurable, $"expected 'cannot tell', got {garage.Percent}%");

            // Either way of saying "cannot tell" is fine: a range that straddles zero, or no
            // range at all because too few listings carried the feature to earn one.
            Assert.False(garage.LowerPercent > 0 || garage.UpperPercent < 0,
                "a not-measurable feature must not come back with a range that clears zero");
        }

        [Fact]
        public void FeatureEffects_ExpensiveMarketAreaFullOfPools_DoesNotGetCreditedToThePool()
        {
            // The confounder that matters most. Half the areas are twice the price and nearly
            // every flat there has a pool; the pool itself is worth nothing anywhere. Compare
            // across areas and a pool looks enormous. Matching never compares across areas.
            var listings = BuildMarket(800, new MarketRules
            {
                Varying = new[] { PremiumFeatures.HasSwimmingPool },
                PoolWorth = 1.0,
                ExpensiveAreaMultiplier = 2.0,
                PoolsConcentratedInExpensiveAreas = true,
            });

            var pool = Measure(listings, PremiumFeatures.HasSwimmingPool);

            Assert.InRange(pool.Percent, -5m, 5m);
        }

        [Fact]
        public void FeatureEffects_FeatureThatAlwaysArrivesWithAnother_IsNotCreditedWithItsWorth()
        {
            // Every flat with a terrace also has a garage, and the garage is the valuable one.
            // Reading terraces off a market average would hand the garage's 20% to the terrace.
            var listings = BuildMarket(800, new MarketRules
            {
                Varying = new[] { PremiumFeatures.HasGarage, PremiumFeatures.HasTerrace },
                GarageWorth = 1.20,
                TerraceWorth = 1.0,
                TerracesAlwaysHaveGarages = true,
            });

            var terrace = Measure(listings, PremiumFeatures.HasTerrace);

            Assert.InRange(terrace.Percent, -6m, 6m);
        }

        [Fact]
        public void FeatureEffects_SeaViewOnTopOfBeachProximity_KeepsTheTwoApart()
        {
            // This is the failure that started the rewrite. Sea view flats sit closer to the
            // beach, and proximity is worth real money on its own. The regression let the two
            // fight over the same signal and paid the sea view a third of its worth.
            var listings = BuildMarket(900, new MarketRules
            {
                Varying = new[] { PremiumFeatures.HasSeaView },
                SeaViewWorth = 1.15,
                BeachHalvingWorth = 1.08,
                SeaViewsSitCloserToTheBeach = true,
            });

            var seaView = Measure(listings, PremiumFeatures.HasSeaView);

            Assert.InRange(seaView.Percent, 11m, 19m);
        }

        [Fact]
        public void FeatureEffects_Balcony_IsNotReportedAsMakingAFlatCheaper()
        {
            // The live table said -1.43% for a balcony. A balcony does not reduce what a flat is
            // worth; that number was collinearity noise being printed as a market fact.
            var listings = BuildMarket(800, new MarketRules { Varying = new[] { PremiumFeatures.HasBalcony }, BalconyWorth = 1.04 });

            var balcony = Measure(listings, PremiumFeatures.HasBalcony);

            Assert.True(balcony.Percent > 0, $"a balcony worth +4% came back as {balcony.Percent}%");

            // Yes/no, and read only against flats with no terrace. Every listing ever collected
            // carries either no balcony or exactly one, so "per balcony" priced a quantity that
            // does not vary; and over half the flats with a terrace also flag a balcony, which
            // let the terrace's worth leak in and come back out as a balcony being a discount.
            Assert.Equal("a balcony without a terrace", balcony.Basis);
        }

        [Fact]
        public void FeatureEffects_Lift_IsWorthMoreHighUpThanOnTheLowerFloors()
        {
            // The complaint that started this: one flat number for a lift, when the whole point
            // of a lift is the floors it saves you climbing.
            var listings = BuildMarket(900, new MarketRules
            {
                Varying = new[] { PremiumFeatures.HasElevator },
                LiftWorth = 1.02,
                LiftIsWorthMoreHighUp = true,
            });

            var lift = Measure(listings, PremiumFeatures.HasElevator);

            Assert.True(lift.IsMeasurable);
            Assert.NotNull(lift.MaximumPercent);
            Assert.True(lift.MaximumPercent > lift.Percent,
                $"a lift high up has to beat one on the ground; got {lift.MaximumPercent}% vs {lift.Percent}%");

            // An "up to" with no stated conditions is a marketing claim, not a measurement.
            Assert.NotNull(lift.MaximumBasis);
            Assert.Contains("floor", lift.MaximumBasis);
        }

        [Fact]
        public void FeatureEffects_LiftWorthTheSameOnEveryFloor_HasNoUpToFigure()
        {
            var listings = BuildMarket(900, new MarketRules
            {
                Varying = new[] { PremiumFeatures.HasElevator },
                LiftWorth = 1.05,
            });

            var lift = Measure(listings, PremiumFeatures.HasElevator);

            Assert.True(lift.IsMeasurable);
            Assert.Null(lift.MaximumPercent);
        }

        [Fact]
        public void FeatureEffects_LiftWorthNothingLowDown_ReportsTheHighFloorFigureAsTheWholeRow()
        {
            // What the real listings say: a lift on the ground, first or second floor is worth
            // nothing measurable. Reporting zero as the headline and hanging "up to 4%" off it
            // would put the only real number in the small print.
            var listings = BuildMarket(900, new MarketRules
            {
                Varying = new[] { PremiumFeatures.HasElevator },
                LiftWorth = 1.08,
                LiftOnlyMattersHighUp = true,
                NoiseScale = 0.1,
            });

            var lift = Measure(listings, PremiumFeatures.HasElevator);

            Assert.True(lift.IsMeasurable, "the high-floor premium is real and has to be reported");
            Assert.InRange(lift.Percent, 5m, 11m);

            // The row now IS the high-floor figure, so there is no second one to quote.
            Assert.Null(lift.MaximumPercent);
            Assert.NotNull(lift.Basis);
            Assert.Contains("floor 3", lift.Basis);
        }


        [Fact]
        public void FeatureEffects_ParkingThatAlwaysComesWithAGarage_IsMeasuredOnTheFlatsWithoutOne()
        {
            // Four listings in five with a garage also carry the parking flag. Read as two
            // free-standing features the columns fought over one signal and parking came back at
            // 0.16% with a range through zero; read as "parking where there is no garage" it is
            // a real, separate thing worth real money.
            var listings = BuildMarket(900, new MarketRules
            {
                Varying = new[] { PremiumFeatures.HasGarage, PremiumFeatures.HasParking },
                GarageWorth = 1.12,
                ParkingWorth = 1.04,
                GaragesAlsoFlagParking = true,
            });

            var parking = Measure(listings, PremiumFeatures.HasParking);
            var garage = Measure(listings, PremiumFeatures.HasGarage);

            Assert.InRange(parking.Percent, 1m, 7m);
            Assert.InRange(garage.Percent, 9m, 15m);
            Assert.Equal("parking without a garage", parking.Basis);
        }

        [Fact]
        public void FeatureEffects_CloseToBeach_IsReportedAsAPlainThreshold()
        {
            // The market puts every flat at either 250m or 2000m from the sea, and 1.06 per
            // halving makes the near ones worth 1.06^3 = +19%. The 500m threshold splits that
            // market exactly down the middle, so the whole 19% is what this row should report.
            var listings = BuildMarket(800, new MarketRules { BeachHalvingWorth = 1.06 });

            var beach = Measure(listings, PremiumFeatures.CloseToBeach);

            Assert.InRange(beach.Percent, 15m, 23m);
            Assert.Equal($"within {ValuationSubject.CloseToBeachMeters}m of the beach", beach.Basis);
        }

        [Fact]
        public void FeatureEffects_CloseToBeach_ListingsWithNoRecordedDistance_AreNotCountedAsFarAway()
        {
            // "We never measured it" is not "it is inland". Reading a missing distance as far
            // from the sea would quietly hand every unmeasured flat the discount.
            var listings = BuildMarket(800, new MarketRules { BeachHalvingWorth = 1.06 });

            foreach (var listing in listings.Take(400))
            {
                listing.DistanceToBeachMeters = null;
            }

            var beach = Measure(listings, PremiumFeatures.CloseToBeach);

            // Only the half that still states a distance counts as evidence.
            Assert.True(beach.ListingsWithFeature <= 400,
                $"unmeasured listings were counted as carriers: {beach.ListingsWithFeature}");

            // And the premium is still read off the ones we can actually compare.
            Assert.True(beach.Percent > 0, $"the beach premium collapsed to {beach.Percent}%");
        }

        [Fact]
        public void FeatureEffects_BathroomWorthEightPercent_ReportsRoughlyEightPercent()
        {
            var listings = BuildMarket(800, new MarketRules { BathroomWorth = 1.08 });

            var bathroom = Measure(listings, PremiumFeatures.ExtraBathroom);

            Assert.InRange(bathroom.Percent, 6m, 10m);
            Assert.Equal("per bathroom", bathroom.Basis);
        }

        [Fact]
        public void FeatureEffects_EnergyGradeWorthFivePercentPerStep_ReportsRoughlyFivePercent()
        {
            var listings = BuildMarket(800, new MarketRules { EnergyStepWorth = 1.05 });

            var energy = Measure(listings, PremiumFeatures.EnergyGrade);

            Assert.InRange(energy.Percent, 3m, 7m);
            Assert.NotNull(energy.Basis);
        }

        [Fact]
        public void FeatureEffects_NewBuild_IsMeasuredAgainstTheOrdinaryCondition()
        {
            var listings = BuildMarket(800, new MarketRules { Varying = new[] { PremiumFeatures.IsNewBuild }, NewBuildWorth = 1.12 });

            var newBuild = Measure(listings, PremiumFeatures.IsNewBuild);

            Assert.InRange(newBuild.Percent, 9m, 15m);
            Assert.True(newBuild.IsMeasurable);
        }

        [Fact]
        public void FeatureEffects_CoverEveryReportedFeatureExactlyOnce()
        {
            var listings = BuildMarket(600, new MarketRules { GarageWorth = 1.10 });

            var effects = FeaturePremiumCalculator.Fit(listings).FeatureEffects;

            Assert.Equal(effects.Count, effects.Select(x => x.Feature).Distinct().Count());

            // Air conditioning must NOT be reported: the source data has thousands of trues and
            // no explicit falses, so any number for it measures whether the advert mentioned it.
            Assert.DoesNotContain(PremiumFeatures.HasAirConditioning, effects.Select(x => x.Feature));

            // Everything the Feature Impact screen expects has to still be there - dropping a row
            // silently is how a feature disappears from the product.
            foreach (var feature in new[]
                     {
                         PremiumFeatures.HasSeaView, PremiumFeatures.HasCityView, PremiumFeatures.HasGarage,
                         PremiumFeatures.HasSwimmingPool, PremiumFeatures.HasTerrace, PremiumFeatures.HasElevator,
                         PremiumFeatures.IsFurnished, PremiumFeatures.HasParking, PremiumFeatures.IsNewBuild,
                         PremiumFeatures.IsRenovated, PremiumFeatures.NeedsRenovation, PremiumFeatures.CloseToBeach,
                         PremiumFeatures.EnergyGrade, PremiumFeatures.ExtraBathroom, PremiumFeatures.FloorLevel,
                         PremiumFeatures.HasBalcony,
                     })
            {
                Assert.Contains(feature, effects.Select(x => x.Feature));
            }

            // The retired one must NOT come back. It is still in the enum so old database rows
            // read back, but nothing should be producing it any more.
            Assert.DoesNotContain(PremiumFeatures.BeachProximity, effects.Select(x => x.Feature));
        }

        [Fact]
        public void FeatureEffects_ListingsWithFeature_CountsOnlyCarriersThatHadSomethingToCompareAgainst()
        {
            // The evidence column. A listing with a pool in a market where every single flat has
            // one proves nothing about pools, and must not be counted as if it did.
            var listings = BuildMarket(600, new MarketRules { GarageWorth = 1.10 });

            var effects = FeaturePremiumCalculator.Fit(listings).FeatureEffects;

            var garage = effects.Single(x => x.Feature == PremiumFeatures.HasGarage);

            Assert.True(garage.ListingsWithFeature > 0, "a market that is half garages must show evidence for them");
            Assert.True(garage.ListingsWithFeature <= 600);

            // Nothing in this market has a city view, so the row has to say so rather than
            // reporting the listings the measurement ran on.
            Assert.Equal(0, effects.Single(x => x.Feature == PremiumFeatures.HasCityView).ListingsWithFeature);
        }

        [Fact]
        public void FeatureEffects_FeatureNothingCanBeComparedOn_ReportsNoRangeRatherThanAPercentage()
        {
            // Every listing furnished: there is no unfurnished flat anywhere to compare against.
            // The honest output is "we cannot measure this", not a number with a range on it.
            var listings = BuildMarket(600, new MarketRules { GarageWorth = 1.10, EverythingIsFurnished = true });

            var furnished = Measure(listings, PremiumFeatures.IsFurnished);

            Assert.False(furnished.IsMeasurable);
            Assert.Equal(0m, furnished.Percent);
        }

        [Fact]
        public void FeatureEffects_AreTheSameEveryTimeTheSameListingsAreMeasured()
        {
            // The confidence ranges come from a bootstrap. Seeded, because a premium that moves
            // when nothing changed is not a measurement - and this screen is recalculated often.
            var listings = BuildMarket(600, new MarketRules { GarageWorth = 1.10, NoiseScale = 0.15 });

            var first = Measure(listings, PremiumFeatures.HasGarage);
            var second = Measure(listings, PremiumFeatures.HasGarage);

            Assert.Equal(first.Percent, second.Percent);
            Assert.Equal(first.LowerPercent, second.LowerPercent);
            Assert.Equal(first.UpperPercent, second.UpperPercent);
        }

        [Fact]
        public void FeatureEffects_SeaView_AlsoReportsWhatItReachesOnTheBeachfront()
        {
            var listings = BuildMarket(900, new MarketRules
            {
                Varying = new[] { PremiumFeatures.HasSeaView },
                SeaViewWorth = 1.10,
                SeaViewIsWorthDoubleOnTheBeachfront = true,
            });

            var seaView = Measure(listings, PremiumFeatures.HasSeaView);

            Assert.NotNull(seaView.MaximumPercent);
            Assert.True(seaView.MaximumPercent > seaView.Percent,
                $"the beachfront figure has to beat the average; got {seaView.MaximumPercent}% vs {seaView.Percent}%");

            // An "up to" with no stated conditions is a marketing claim, not a measurement.
            Assert.NotNull(seaView.MaximumBasis);
            Assert.Contains("beach", seaView.MaximumBasis);
        }

        [Fact]
        public void FeatureEffects_SeaViewWorthTheSameEverywhere_HasNoUpToFigure()
        {
            var listings = BuildMarket(900, new MarketRules { Varying = new[] { PremiumFeatures.HasSeaView }, SeaViewWorth = 1.10 });

            var seaView = Measure(listings, PremiumFeatures.HasSeaView);

            Assert.True(seaView.IsMeasurable);
            Assert.Null(seaView.MaximumPercent);
        }

        [Fact]
        public void FeatureEffects_YesNoFeatures_CarryNoBasisNote()
        {
            var listings = BuildMarket(600, new MarketRules { GarageWorth = 1.10 });

            var effects = FeaturePremiumCalculator.Fit(listings).FeatureEffects;

            Assert.Null(effects.Single(x => x.Feature == PremiumFeatures.HasGarage).Basis);
            Assert.Null(effects.Single(x => x.Feature == PremiumFeatures.NeedsRenovation).Basis);

            // A basis note is there whenever "if present" would mislead. That is every quantity,
            // whose percentage is meaningless unlabelled - and Close to Beach, which is a plain
            // yes/no but needs to say WHICH distance counts, or the row is just a claim.
            foreach (var feature in new[]
                     {
                         PremiumFeatures.CloseToBeach, PremiumFeatures.EnergyGrade,
                         PremiumFeatures.ExtraBathroom, PremiumFeatures.FloorLevel, PremiumFeatures.HasBalcony,
                     })
            {
                Assert.NotNull(effects.Single(x => x.Feature == feature).Basis);
            }
        }

        /// <summary>
        /// What the premiums come out as on the listings actually collected. Asserts nothing -
        /// against real data there is no known answer to assert against - so it exists for a
        /// human to read the numbers and judge whether they are believable.
        ///
        /// It also prints the beach premium the naive way, by simply FILTERING the listings into
        /// "within 500m" and "further out" and comparing their median prices. That is the obvious
        /// way to answer the question, and printing the two side by side is the clearest evidence
        /// of why the grouping exists: the filtered figure also contains every other way a
        /// beachfront flat differs from an inland one - newer, in a pricier market area, far more
        /// likely to have a sea view - and reports the lot as the worth of being near the sea.
        ///
        /// Hits a real database, so it is opt-in the same way <see cref="ValuationBacktest"/> is:
        ///   $env:STAYPILOT_BACKTEST=1; dotnet test --filter FullyQualifiedName~RealListings
        /// </summary>
        [Fact]
        public void Premiums_AgainstTheRealListings_ReadForBelievability()
        {
            if (Environment.GetEnvironmentVariable("STAYPILOT_BACKTEST") != "1")
                return;

            var options = new DbContextOptionsBuilder<StayPilotDbContext>().UseSqlServer(ConnectionString).Options;

            using var context = new StayPilotDbContext(options);

            var listings = context.PropertyListings
                .AsNoTracking()
                .Include(x => x.MarketArea)
                .Include(x => x.ListingSnapshots)
                .ToList();

            var measured = FeaturePremiumCalculator.Fit(listings);

            _output.WriteLine($"{listings.Count} listings loaded, {measured.TrainingListings} usable.");
            _output.WriteLine("");
            _output.WriteLine($"{"feature",-18} {"premium",9} {"range",16} {"evidence",9}   basis");
            _output.WriteLine(new string('-', 90));

            foreach (var effect in measured.FeatureEffects.OrderByDescending(x => x.Percent))
            {
                // Printed even for rows that cannot be measured, because "-3.0 to 4.0" and
                // "0.0 to 0.0" mean different things: one is a range that happens to include zero,
                // the other is no measurement at all.
                var range = $"{effect.LowerPercent,6:F1} to {effect.UpperPercent,-6:F1}"
                    + (effect.IsMeasurable ? "" : " (incl. 0)");

                _output.WriteLine(
                    $"{effect.Feature,-18} {effect.Percent,9:F1} {range,16} {effect.ListingsWithFeature,9}   " +
                    $"{effect.Basis ?? "if present"}");
            }

            _output.WriteLine("");

            foreach (var effect in measured.FeatureEffects.Where(x => x.MaximumPercent.HasValue))
            {
                _output.WriteLine($"{effect.Feature}: up to {effect.MaximumPercent:F1}% {effect.MaximumBasis}.");
            }

            ReportTheNaiveBeachPremium(listings);
        }

        /// <summary>
        /// The beach premium worked out by filtering alone - no grouping, no holding anything
        /// else still. Printed next to the measured figure so the gap between them is visible.
        /// </summary>
        private void ReportTheNaiveBeachPremium(List<PropertyListing> listings)
        {
            var priced = ListingQuality.UsableSubjects(listings)
                .Where(x => ValuationSubject.KnowsBeachDistance(x.Subject))
                .Select(x => (Close: ValuationSubject.IsCloseToBeach(x.Subject), PricePerM2: Math.Exp(x.LogPricePerM2)))
                .ToList();

            var close = priced.Where(x => x.Close).Select(x => x.PricePerM2).OrderBy(x => x).ToList();
            var away = priced.Where(x => !x.Close).Select(x => x.PricePerM2).OrderBy(x => x).ToList();

            if (close.Count == 0 || away.Count == 0)
                return;

            var naive = (close[close.Count / 2] / away[away.Count / 2] - 1) * 100;

            var measured = FeaturePremiumCalculator.Fit(listings).FeatureEffects
                .Single(x => x.Feature == PremiumFeatures.CloseToBeach);

            _output.WriteLine("");
            _output.WriteLine("=== CLOSE TO BEACH: filtered vs compared ===");
            _output.WriteLine($"  just filtering  {naive,6:F1}%   " +
                              $"({close.Count:N0} within 500m at {close[close.Count / 2]:N0}/m2, " +
                              $"{away.Count:N0} beyond at {away[away.Count / 2]:N0}/m2)");
            _output.WriteLine($"  compared        {measured.Percent,6:F1}%   " +
                              $"({measured.LowerPercent:F1} to {measured.UpperPercent:F1}, " +
                              $"{measured.ListingsWithFeature:N0} carriers)");
            _output.WriteLine("");
            _output.WriteLine("  The gap is everything else a beachfront flat is: newer, pricier area,");
            _output.WriteLine("  far likelier to have a sea view. Filtering alone bills all of it to the beach.");
        }

        private static FeatureEffect Measure(List<PropertyListing> listings, PremiumFeatures feature)
        {
            return FeaturePremiumCalculator.Fit(listings).FeatureEffects.Single(x => x.Feature == feature);
        }

        /// <summary>What a synthetic market is worth paying for, and what is tangled with what.</summary>
        private sealed class MarketRules
        {
            /// <summary>
            /// Which yes/no features exist at all here. Everything else is false on every
            /// listing, which is what keeps strata big enough to compare inside: a market where
            /// all eight features vary at random is 256 combinations, and a few hundred listings
            /// spread over those is a pile of strata of one. Real markets are lumpier than that,
            /// but the fixtures should not pretend the limit does not exist.
            /// </summary>
            public PremiumFeatures[] Varying { get; init; } = { PremiumFeatures.HasGarage };

            /// <summary>
            /// How common the varying features are. Half is the comfortable case; drop it and
            /// the evidence thins out, which is how a market that genuinely cannot answer the
            /// question gets built.
            /// </summary>
            public double Prevalence { get; init; } = 0.5;

            public double GarageWorth { get; init; } = 1.0;

            public double ParkingWorth { get; init; } = 1.0;

            public double LiftWorth { get; init; } = 1.0;

            public double PoolWorth { get; init; } = 1.0;

            public double TerraceWorth { get; init; } = 1.0;

            public double SeaViewWorth { get; init; } = 1.0;

            public double BalconyWorth { get; init; } = 1.0;

            public double BathroomWorth { get; init; } = 1.0;

            public double EnergyStepWorth { get; init; } = 1.0;

            public double BeachHalvingWorth { get; init; } = 1.0;

            public double NewBuildWorth { get; init; } = 1.0;

            public double NoiseScale { get; init; }

            /// <summary>How much dearer the expensive half of the areas is, at identical everything.</summary>
            public double ExpensiveAreaMultiplier { get; init; } = 1.0;

            /// <summary>Pools cluster in the expensive areas, so a naive read blames the pool.</summary>
            public bool PoolsConcentratedInExpensiveAreas { get; init; }

            /// <summary>Terraces only ever arrive with a garage, so the two are tangled together.</summary>
            public bool TerracesAlwaysHaveGarages { get; init; }

            /// <summary>Sea view flats sit nearer the water, which is worth money by itself.</summary>
            public bool SeaViewsSitCloserToTheBeach { get; init; }

            /// <summary>The view is worth double within 500m and ordinary inland.</summary>
            public bool SeaViewIsWorthDoubleOnTheBeachfront { get; init; }

            /// <summary>The lift is worth three times as much from the third floor up.</summary>
            public bool LiftIsWorthMoreHighUp { get; init; }

            /// <summary>
            /// The lift is worth nothing at all below the third floor - which is what the real
            /// listings say, and the case where the high-floor figure has to become the row
            /// rather than an "up to" hung off a headline of zero.
            /// </summary>
            public bool LiftOnlyMattersHighUp { get; init; }

            /// <summary>
            /// Every flat with a garage also carries the parking flag, so read as two separate
            /// features the two are tangled and parking reads as worth nothing.
            /// </summary>
            public bool GaragesAlsoFlagParking { get; init; }

            /// <summary>No unfurnished flat exists, so there is nothing to compare against.</summary>
            public bool EverythingIsFurnished { get; init; }
        }

        /// <summary>A listing with only the fields the quality gate looks at.</summary>
        private static PropertyListing NewListing(int id, int areaM2, decimal pricePerM2)
        {
            return new PropertyListing
            {
                Id = id,
                MarketAreaId = 1,
                Typology = Typology.T2,
                PropertyType = PropertyType.Apartment,
                AreaM2 = areaM2,
                SourceUrl = $"https://example.test/{id}",
                ListingSnapshots = new List<ListingSnapshot>
                {
                    new()
                    {
                        PricePerM2 = pricePerM2,
                        Price = pricePerM2 * Math.Max(1, areaM2),
                        SnapshotDateUtc = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
                    },
                },
            };
        }

        /// <summary>
        /// Spread across this many market areas, so there are enough strata to bootstrap. One
        /// area would give a single stratum per feature combination, and the calculator would -
        /// quite correctly - report that it cannot measure anything.
        /// </summary>
        private const int MarketAreas = 30;

        /// <summary>
        /// Builds a market where the answer is known by construction, and where listings alike in
        /// every way except one actually exist. Every listing shares a typology, property type
        /// and size band, so what separates strata is the market area plus whichever features
        /// <see cref="MarketRules.Varying"/> turns on. The quantities - bathrooms, floor, energy
        /// grade, beach distance - vary freely, because the calculator adjusts for those inside a
        /// stratum instead of matching on them.
        /// </summary>
        private static List<PropertyListing> BuildMarket(int count, MarketRules rules)
        {
            var noise = new Random(20260814);
            var quantities = new Random(4242);

            // A separate draw stream per feature, so turning one on cannot shift another's draws
            // and quietly move an unrelated test's numbers.
            var streams = new Dictionary<PremiumFeatures, Random>();
            var listings = new List<PropertyListing>();

            bool Draw(PremiumFeatures feature, double? probability = null)
            {
                if (!rules.Varying.Contains(feature))
                    return false;

                if (!streams.TryGetValue(feature, out var stream))
                {
                    stream = new Random((int)feature * 7919);
                    streams[feature] = stream;
                }

                return stream.NextDouble() < (probability ?? rules.Prevalence);
            }

            for (var i = 0; i < count; i++)
            {
                var marketAreaId = 1 + (i % MarketAreas);
                var isExpensiveArea = marketAreaId % 2 == 0;

                var hasGarage = Draw(PremiumFeatures.HasGarage);
                var hasSeaView = Draw(PremiumFeatures.HasSeaView);
                var balconies = Draw(PremiumFeatures.HasBalcony) ? 1 : 0;
                var isNewBuild = Draw(PremiumFeatures.IsNewBuild);

                // A terrace that only ever arrives with a garage. Matching still separates the
                // two, because the garage is held still while the terrace is measured.
                var hasTerrace = Draw(PremiumFeatures.HasTerrace) && (!rules.TerracesAlwaysHaveGarages || hasGarage);

                // Pools cluster where property is dear. Read across areas that looks like a large
                // pool premium; read inside one area it is worth exactly what it is worth.
                var hasPool = rules.PoolsConcentratedInExpensiveAreas
                    ? Draw(PremiumFeatures.HasSwimmingPool, isExpensiveArea ? 0.8 : 0.15)
                    : Draw(PremiumFeatures.HasSwimmingPool);

                var bathrooms = 1 + quantities.Next(3);
                var energyScore = quantities.Next(9);
                var floor = quantities.Next(5);

                // Parking that always arrives alongside a garage: the tangle that made parking
                // read as worth nothing until it was measured on the flats with no garage.
                var hasParking = Draw(PremiumFeatures.HasParking) || (rules.GaragesAlsoFlagParking && hasGarage);

                var hasLift = Draw(PremiumFeatures.HasElevator);

                // A lift is worth what the stairs would have cost you, so it is worth more the
                // further up the flat is. One flat number for it hides exactly this.
                var liftMultiplier = hasLift && (!rules.LiftOnlyMattersHighUp || floor >= 3)
                    ? 1 + ((rules.LiftWorth - 1) * (rules.LiftIsWorthMoreHighUp && floor >= 3 ? 3.0 : 1.0))
                    : 1.0;

                // Sea view flats sitting nearer the water is the tangle that broke the old
                // regression: proximity is worth money by itself, and the two fought over it.
                var beachMeters = rules.SeaViewsSitCloserToTheBeach && hasSeaView
                    ? 250
                    : (quantities.Next(2) == 0 ? 250 : 2000);

                var seaViewMultiplier = 1.0;

                if (hasSeaView)
                {
                    var beachfrontBonus = rules.SeaViewIsWorthDoubleOnTheBeachfront && beachMeters <= 500 ? 2.0 : 1.0;

                    seaViewMultiplier = 1 + ((rules.SeaViewWorth - 1) * beachfrontBonus);
                }

                var pricePerM2 = (double)BasePricePerM2
                    * (hasGarage ? rules.GarageWorth : 1.0)
                    * liftMultiplier

                    // A garage already includes somewhere to put the car, so parking is only
                    // worth anything of its own where there is no garage.
                    * (hasParking && !hasGarage ? rules.ParkingWorth : 1.0)
                    * (hasPool ? rules.PoolWorth : 1.0)
                    * (hasTerrace ? rules.TerraceWorth : 1.0)
                    * (isNewBuild ? rules.NewBuildWorth : 1.0)
                    * seaViewMultiplier
                    * Math.Pow(rules.BalconyWorth, balconies)
                    * Math.Pow(rules.BathroomWorth, bathrooms)
                    * Math.Pow(rules.EnergyStepWorth, energyScore)
                    * Math.Pow(rules.BeachHalvingWorth, Math.Log2(2000.0 / beachMeters))
                    * (isExpensiveArea ? rules.ExpensiveAreaMultiplier : 1.0)
                    * Math.Exp(rules.NoiseScale * (noise.NextDouble() - 0.5));

                // 70-72m² keeps every listing inside one 25m² band, so the band never splits a
                // stratum. What variation is left is what the size column is there for.
                var areaM2 = 70 + (i % 3);

                listings.Add(new PropertyListing
                {
                    Id = i + 1,
                    SourceUrl = $"https://example.test/{i + 1}",
                    MarketAreaId = marketAreaId,
                    PropertyType = PropertyType.Apartment,
                    Typology = Typology.T2,
                    Condition = isNewBuild ? PropertyCondition.NewBuild : PropertyCondition.Good,
                    AreaM2 = areaM2,
                    Bathrooms = bathrooms,
                    BalconyCount = balconies,
                    EnergyCertificate = EnergyLetter(energyScore),
                    Floor = floor,
                    ConstructionYear = 1990 + (i % 30),
                    DistanceToBeachMeters = beachMeters,
                    Latitude = 37.08m + (i % 20 * 0.0005m),
                    Longitude = -8.10m + (i % 17 * 0.0005m),
                    HasGarage = hasGarage,
                    HasParking = hasParking,
                    HasElevator = hasLift,
                    HasTerrace = hasTerrace,
                    HasSeaView = hasSeaView,
                    HasSwimmingPool = hasPool,
                    IsFurnished = rules.EverythingIsFurnished,
                    ListingSnapshots = new List<ListingSnapshot>
                    {
                        new()
                        {
                            Id = i + 1,
                            PropertyListingId = i + 1,
                            PricePerM2 = (decimal)pricePerM2,
                            Price = (decimal)pricePerM2 * areaM2,
                            SnapshotDateUtc = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
                        },
                    },
                });
            }

            return listings;
        }

        /// <summary>The letter for a position on the energy scale: 0 is G, 7 is A.</summary>
        private static string EnergyLetter(int score) => score switch
        {
            0 => "G", 1 => "F", 2 => "E", 3 => "D", 4 => "C", 5 => "B-", 6 => "B", _ => "A",
        };
    }
}
