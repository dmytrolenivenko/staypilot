using StayPilot.Application.Helpers.Calculators;
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;

namespace StayPilot.UnitTests
{
    /// <summary>
    /// These tests build a synthetic market where the true answer is known by construction -
    /// a garage really is worth exactly 10%, and nothing else moves the price - then check the
    /// model recovers it. That is the only way to test a statistical model: against real data
    /// there is nothing to compare the output to.
    /// </summary>
    public class ValuationModelTests
    {
        private const decimal BasePricePerM2 = 5000m;

        [Fact]
        public void Fit_TooFewListings_Throws()
        {
            var listings = BuildMarket(50, garageWorth: 1.10, noiseScale: 0);

            var error = Assert.Throws<InvalidOperationException>(() => ValuationModel.Fit(listings));

            Assert.Contains("at least", error.Message);
        }

        [Fact]
        public void Fit_ListingsWithoutSnapshotsOrArea_AreSkippedRatherThanFitted()
        {
            var listings = BuildMarket(400, garageWorth: 1.10, noiseScale: 0);

            // Two broken rows: no price history, and a zero floor area. Left in the fit they
            // would drag the whole model; they must simply be ignored.
            listings.Add(new PropertyListing { Id = 9001, AreaM2 = 60, MarketAreaId = 1 });
            listings.Add(new PropertyListing
            {
                Id = 9002,
                AreaM2 = 0,
                MarketAreaId = 1,
                ListingSnapshots = new List<ListingSnapshot> { new() { PricePerM2 = 5000m, Price = 1m } }
            });

            var model = ValuationModel.Fit(listings);

            Assert.Equal(400, model.TrainingListings);
        }

        [Fact]
        public void FeatureEffects_GarageWorthTenPercent_ReportsRoughlyTenPercent()
        {
            var listings = BuildMarket(400, garageWorth: 1.10, noiseScale: 0);

            var model = ValuationModel.Fit(listings);
            var garage = model.FeatureEffects.Single(x => x.Feature == PremiumFeatures.HasGarage);

            Assert.InRange(garage.Percent, 9m, 11m);
            Assert.True(garage.IsMeasurable, "a clean 10% effect must come back as measurable");
        }

        [Fact]
        public void FeatureEffects_FeatureThatChangesNothing_IsReportedAsNotMeasurable()
        {
            // Garage is assigned to half the listings but does not move the price at all, and
            // the prices carry real noise. The honest answer is "cannot tell", not "0.0%".
            var listings = BuildMarket(400, garageWorth: 1.0, noiseScale: 0.25);

            var model = ValuationModel.Fit(listings);
            var garage = model.FeatureEffects.Single(x => x.Feature == PremiumFeatures.HasGarage);

            Assert.False(garage.IsMeasurable, $"expected 'cannot tell', got {garage.Percent}%");
            Assert.True(garage.LowerPercent < 0 && garage.UpperPercent > 0,
                "a not-measurable feature's confidence range has to straddle zero");
        }

        [Fact]
        public void FeatureEffects_BeachProximity_IsReportedPerHalvingOfDistance()
        {
            // Price scales so that halving the distance to the beach is worth exactly 5%.
            var listings = BuildMarket(400, garageWorth: 1.0, noiseScale: 0, beachHalvingWorth: 1.05);

            var model = ValuationModel.Fit(listings);
            var beach = model.FeatureEffects.Single(x => x.Feature == PremiumFeatures.BeachProximity);

            Assert.InRange(beach.Percent, 4m, 6m);
        }

        [Fact]
        public void FeatureEffects_CoverEveryReportedFeatureExactlyOnce()
        {
            var listings = BuildMarket(400, garageWorth: 1.10, noiseScale: 0);

            var model = ValuationModel.Fit(listings);

            Assert.Equal(model.FeatureEffects.Count, model.FeatureEffects.Select(x => x.Feature).Distinct().Count());

            // Air conditioning must NOT be reported: the source data has no explicit falses, so
            // any number for it would really be measuring whether the advert mentioned it.
            Assert.DoesNotContain(PremiumFeatures.HasAirConditioning, model.FeatureEffects.Select(x => x.Feature));

            Assert.Contains(PremiumFeatures.BeachProximity, model.FeatureEffects.Select(x => x.Feature));
        }

        [Fact]
        public void FeatureEffects_ListingsWithFeature_CountsOnlyTheListingsThatActuallyHaveIt()
        {
            // The screen used to print the training total against every row, so a sea view read
            // off a third of the market looked as well-evidenced as a garage read off half of it.
            var listings = BuildMarket(400, garageWorth: 1.10, noiseScale: 0);

            var model = ValuationModel.Fit(listings);

            // BuildMarket gives every other listing a garage and every third one a sea view.
            Assert.Equal(200, model.FeatureEffects.Single(x => x.Feature == PremiumFeatures.HasGarage).ListingsWithFeature);
            Assert.Equal(134, model.FeatureEffects.Single(x => x.Feature == PremiumFeatures.HasSeaView).ListingsWithFeature);

            // Nothing in this market has a pool, and the count has to say so rather than
            // reporting the 400 listings the fit ran on.
            Assert.Equal(0, model.FeatureEffects.Single(x => x.Feature == PremiumFeatures.HasSwimmingPool).ListingsWithFeature);
        }

        [Fact]
        public void FeatureEffects_BeachProximity_CountsOnlyListingsWithAUsableDistance()
        {
            // Beach distance is not a yes/no feature, so its count is "how many listings actually
            // told us a distance". The missing and the broken ones were filled in with the median
            // - counting them would claim evidence that is not there.
            var listings = BuildMarket(400, garageWorth: 1.0, noiseScale: 0, beachHalvingWorth: 1.05);

            foreach (var listing in listings.Take(10))
            {
                listing.DistanceToBeachMeters = null;
            }

            foreach (var listing in listings.Skip(10).Take(5))
            {
                listing.DistanceToBeachMeters = 1_353_393;      // the broken-longitude distance
            }

            var model = ValuationModel.Fit(listings);
            var beach = model.FeatureEffects.Single(x => x.Feature == PremiumFeatures.BeachProximity);

            Assert.Equal(385, beach.ListingsWithFeature);
            Assert.Equal(400, model.TrainingListings);
        }

        [Fact]
        public void FeatureEffects_SeaView_AlsoReportsWhatItReachesOnTheBeachfront()
        {
            // The headline sea view figure averages beachfront views in with adverts 5km inland,
            // which is why it lands near a garage. The row has to carry the beachfront number too,
            // and say what conditions it holds under.
            var listings = BuildMarket(600, garageWorth: 1.0, noiseScale: 0, seaViewAt100mWorth: 1.25, seaViewDecaysWithDistance: true);

            var model = ValuationModel.Fit(listings);
            var seaView = model.FeatureEffects.Single(x => x.Feature == PremiumFeatures.HasSeaView);

            Assert.NotNull(seaView.MaximumPercent);
            Assert.True(seaView.MaximumPercent > seaView.Percent,
                $"the beachfront figure has to beat the average; got {seaView.MaximumPercent}% vs {seaView.Percent}%");

            // An "up to" with no stated conditions is a marketing claim, not a measurement.
            Assert.NotNull(seaView.MaximumBasis);
            Assert.Contains("beach", seaView.MaximumBasis);

            // It is the same fitted curve, read at the waterfront - not a second, rosier model.
            Assert.Equal(model.SeaViewPercentAt(100), seaView.MaximumPercent!.Value, precision: 2);
        }

        [Fact]
        public void FeatureEffects_SeaViewTheDataCannotMeasure_HasNoUpToFigure()
        {
            // No sea view premium in this market at all. Hanging an "up to" off a row whose
            // confidence range straddles zero would dress noise up as a ceiling.
            var listings = BuildMarket(400, garageWorth: 1.10, noiseScale: 0.25);

            var model = ValuationModel.Fit(listings);
            var seaView = model.FeatureEffects.Single(x => x.Feature == PremiumFeatures.HasSeaView);

            Assert.False(seaView.IsMeasurable, $"expected 'cannot tell', got {seaView.Percent}%");
            Assert.Null(seaView.MaximumPercent);
            Assert.Null(seaView.MaximumBasis);
        }

        [Fact]
        public void FeatureEffects_FeatureWorthTheSameEverywhere_HasNoUpToFigure()
        {
            // A garage is a garage. Only features whose worth genuinely varies with the property
            // get a second number; giving every row one would make "up to" meaningless.
            var listings = BuildMarket(400, garageWorth: 1.10, noiseScale: 0);

            var model = ValuationModel.Fit(listings);
            var garage = model.FeatureEffects.Single(x => x.Feature == PremiumFeatures.HasGarage);

            Assert.True(garage.IsMeasurable);
            Assert.Null(garage.MaximumPercent);
        }

        [Fact]
        public void FeatureEffects_EnergyGradeWorthFivePercentPerStep_ReportsRoughlyFivePercent()
        {
            // The energy grade is read off a column the model only just gained, so this doubles
            // as the check that the column is where BuildRow puts it: point EnergyGradeColumn at
            // the wrong index and this recovers somebody else's coefficient, not 5%.
            var listings = BuildMarket(600, garageWorth: 1.0, noiseScale: 0, energyStepWorth: 1.05);

            var model = ValuationModel.Fit(listings);
            var energy = model.FeatureEffects.Single(x => x.Feature == PremiumFeatures.EnergyGrade);

            Assert.InRange(energy.Percent, 4m, 6m);
            Assert.True(energy.IsMeasurable);

            // A percentage per step means nothing unless the row says "per step".
            Assert.NotNull(energy.Basis);
        }

        [Fact]
        public void FeatureEffects_BathroomWorthEightPercent_ReportsRoughlyEightPercent()
        {
            // Same column-index check for the scalar block's other end.
            var listings = BuildMarket(600, garageWorth: 1.0, noiseScale: 0, bathroomWorth: 1.08);

            var model = ValuationModel.Fit(listings);
            var bathroom = model.FeatureEffects.Single(x => x.Feature == PremiumFeatures.ExtraBathroom);

            Assert.InRange(bathroom.Percent, 7m, 9m);
            Assert.Equal("per bathroom", bathroom.Basis);
        }

        [Fact]
        public void FeatureEffects_MeasuredFeatures_AreAllReportedWithTheRightKindOfBasis()
        {
            var listings = BuildMarket(600, garageWorth: 1.10, noiseScale: 0);

            var model = ValuationModel.Fit(listings);

            // Every feature whose premium is per unit of something has to say so; a bare
            // percentage on "per floor up" reads as "a flat with floors", which is every flat.
            foreach (var feature in new[]
                     {
                         PremiumFeatures.EnergyGrade, PremiumFeatures.ExtraBathroom,
                         PremiumFeatures.FloorLevel, PremiumFeatures.HasBalcony,
                     })
            {
                Assert.NotNull(model.FeatureEffects.Single(x => x.Feature == feature).Basis);
            }

            // Needing renovation is an ordinary yes/no, so it falls back to "if present".
            Assert.Null(model.FeatureEffects.Single(x => x.Feature == PremiumFeatures.NeedsRenovation).Basis);
        }

        [Fact]
        public void FeatureEffects_EnergyGradeNotStated_IsNotScoredAsAnAverageRating()
        {
            // A listing with no certificate is not a listing with a middling one. The model
            // carries a "not stated" flag for exactly this, and without it the missing 7% would
            // drag the grade's coefficient toward zero.
            var listings = BuildMarket(600, garageWorth: 1.0, noiseScale: 0, energyStepWorth: 1.05);

            foreach (var listing in listings.Take(60))
            {
                listing.EnergyCertificate = null;
            }

            var model = ValuationModel.Fit(listings);
            var energy = model.FeatureEffects.Single(x => x.Feature == PremiumFeatures.EnergyGrade);

            Assert.InRange(energy.Percent, 4m, 6m);

            // The count is the evidence, so the 60 without a certificate must not be in it.
            Assert.Equal(540, energy.ListingsWithFeature);
        }

        [Fact]
        public void SeaViewPercentAt_IsWorthMoreNearTheBeachThanInland()
        {
            // A sea view from the beachfront and a sea view from 5km inland are not the same
            // claim, and the model carries a SeaView x distance term to say so. The Feature
            // Impact screen shows only the average; valuations use this.
            var listings = BuildMarket(600, garageWorth: 1.0, noiseScale: 0, seaViewAt100mWorth: 1.25, seaViewDecaysWithDistance: true);

            var model = ValuationModel.Fit(listings);

            var closeToBeach = model.SeaViewPercentAt(100);
            var farInland = model.SeaViewPercentAt(5000);

            Assert.True(closeToBeach > farInland + 5m,
                $"a beachfront sea view should clearly beat an inland one; got {closeToBeach}% vs {farInland}%");
        }

        [Fact]
        public void SeaViewPercentAt_ImplausibleDistance_FallsBackInsteadOfGoingWild()
        {
            var listings = BuildMarket(600, garageWorth: 1.0, noiseScale: 0, seaViewAt100mWorth: 1.25, seaViewDecaysWithDistance: true);

            var model = ValuationModel.Fit(listings);

            // The broken-longitude property reported 1,353,393m from the beach.
            var broken = model.SeaViewPercentAt(1_353_393);

            Assert.InRange(broken, -50m, 100m);
        }

        [Fact]
        public void FeatureEffects_YesNoFeatures_CarryNoBasisNote()
        {
            // The distance curve stays inside the valuation; the market summary screen shows one
            // average per feature and should not explain itself row by row.
            var listings = BuildMarket(400, garageWorth: 1.10, noiseScale: 0);

            var model = ValuationModel.Fit(listings);

            Assert.Null(model.FeatureEffects.Single(x => x.Feature == PremiumFeatures.HasSeaView).Basis);
            Assert.Null(model.FeatureEffects.Single(x => x.Feature == PremiumFeatures.HasGarage).Basis);

            // Beach proximity is the exception: its percentage is meaningless unlabelled.
            Assert.NotNull(model.FeatureEffects.Single(x => x.Feature == PremiumFeatures.BeachProximity).Basis);
        }

        [Fact]
        public void MarketAverages_DescribeTheListingsTheModelWasFittedOn()
        {
            // These exist so a valuation can say what "three bathrooms" is worth ABOVE the
            // typical property. If any of them came back zero the breakdown would measure
            // against nothing and pay every property a premium for being ordinary - so the
            // assertion that matters here is that they describe the market, not that they exist.
            var listings = BuildMarket(400, garageWorth: 1.10, noiseScale: 0);

            var model = ValuationModel.Fit(listings);

            // BuildMarket draws 1-3 bathrooms and 0-1 balconies.
            Assert.InRange(model.MarketAverageBathrooms, 1.5, 2.5);
            Assert.InRange(model.MarketAverageBalconies, 0.2, 0.8);

            // Beach distances run 100m to 7.0km, so the median sits well inside that.
            Assert.InRange(model.MarketMedianBeachMeters, 100, 7000);

            // The energy scale is 0 (G) to 8 (A+); the drawn grades average near its middle.
            Assert.InRange(model.MarketAverageEnergyGrade, 1.0, 7.0);
        }

        [Fact]
        public void MarketAverages_AreMeasuredOnlyOnListingsThatStatedTheValue()
        {
            // The floor median must not be dragged toward zero by the listings that never said
            // which floor they were on. A "typical floor" of 0 would make every third-floor flat
            // look three storeys above the market and quietly inflate its valuation.
            var listings = BuildMarket(400, garageWorth: 1.10, noiseScale: 0);

            foreach (var listing in listings.Take(200))
            {
                listing.Floor = null;
            }

            var model = ValuationModel.Fit(listings);

            Assert.True(model.MarketMedianFloor > 0,
                $"expected the median floor to ignore the unstated ones, got {model.MarketMedianFloor}");
        }

        [Fact]
        public void PredictPricePerM2_PropertyMatchingTheMarket_LandsNearTheTruePrice()
        {
            var listings = BuildMarket(400, garageWorth: 1.10, noiseScale: 0);

            var model = ValuationModel.Fit(listings);

            // A no-garage flat at the market's baseline should come back near the base price.
            var subject = BuildSubject(hasGarage: false, beachMeters: 800);
            var prediction = model.PredictPricePerM2(subject);

            Assert.InRange(prediction.PricePerM2, BasePricePerM2 * 0.9m, BasePricePerM2 * 1.1m);
        }

        [Fact]
        public void PredictPricePerM2_SameFlatWithAGarage_IsWorthAboutTenPercentMore()
        {
            var listings = BuildMarket(400, garageWorth: 1.10, noiseScale: 0);

            var model = ValuationModel.Fit(listings);

            var withoutGarage = model.PredictPricePerM2(BuildSubject(hasGarage: false, beachMeters: 800));
            var withGarage = model.PredictPricePerM2(BuildSubject(hasGarage: true, beachMeters: 800));

            var ratio = withGarage.PricePerM2 / withoutGarage.PricePerM2;

            Assert.InRange(ratio, 1.08m, 1.12m);
        }

        [Fact]
        public void PredictPricePerM2_PropertyNowhereNearAnyListing_ReportsThatItHasNoLocalEvidence()
        {
            // Every listing collected so far is in the Algarve. A property in Porto must not
            // come back looking well-supported - this is what the confidence level hangs on.
            var listings = BuildMarket(400, garageWorth: 1.10, noiseScale: 0);

            var model = ValuationModel.Fit(listings);

            var faraway = BuildSubject(hasGarage: false, beachMeters: 800);
            faraway.Latitude = 41.15m;
            faraway.Longitude = -8.61m;

            var prediction = model.PredictPricePerM2(faraway);

            Assert.True(prediction.NearestComparableMeters > 100_000,
                $"expected the nearest comp to be far away, got {prediction.NearestComparableMeters:F0}m");
        }

        [Fact]
        public void PredictPricePerM2_PropertyNowhereNearAnyListing_DoesNotBorrowADistantNeighbourhoodCorrection()
        {
            // Regression test for a real miss: a property whose longitude lost its minus sign
            // sat in the Mediterranean, and the model happily applied the median error of the
            // ten "nearest" listings a thousand kilometres away.
            var listings = BuildMarket(400, garageWorth: 1.10, noiseScale: 0.2);

            var model = ValuationModel.Fit(listings);

            var faraway = BuildSubject(hasGarage: false, beachMeters: 800);
            faraway.Latitude = 37.55m;
            faraway.Longitude = 8.55m;               // should be -8.55; this is the bug we saw

            var prediction = model.PredictPricePerM2(faraway);

            Assert.Equal(0, prediction.LocalComparablesUsed);
        }

        [Fact]
        public void PredictPricePerM2_ImplausibleBeachDistance_IsIgnoredRatherThanBelieved()
        {
            // 1,353km from the beach is not a location, it is a broken coordinate. Fed to a log
            // term it produced a huge bogus discount; it has to be treated as simply unknown.
            var listings = BuildMarket(400, garageWorth: 1.0, noiseScale: 0, beachHalvingWorth: 1.08);

            var model = ValuationModel.Fit(listings);

            var sane = BuildSubject(hasGarage: false, beachMeters: 800);

            var broken = BuildSubject(hasGarage: false, beachMeters: 1_353_393);
            var brokenPrediction = model.PredictPricePerM2(broken);

            var unknown = BuildSubject(hasGarage: false, beachMeters: 800);
            unknown.DistanceToBeachMeters = null;
            var unknownPrediction = model.PredictPricePerM2(unknown);

            // The nonsense value must land on the same answer as "we do not know", and nowhere
            // near the collapse it used to produce.
            Assert.Equal(unknownPrediction.PricePerM2, brokenPrediction.PricePerM2, precision: 2);

            var sanePrediction = model.PredictPricePerM2(sane);
            Assert.InRange(brokenPrediction.PricePerM2, sanePrediction.PricePerM2 * 0.7m, sanePrediction.PricePerM2 * 1.3m);
        }

        [Fact]
        public void PredictPricePerM2_PropertyWithNoCoordinates_StillPricesWithoutLocalEvidence()
        {
            var listings = BuildMarket(400, garageWorth: 1.10, noiseScale: 0);

            var model = ValuationModel.Fit(listings);

            var subject = BuildSubject(hasGarage: false, beachMeters: 800);
            subject.Latitude = null;
            subject.Longitude = null;

            var prediction = model.PredictPricePerM2(subject);

            Assert.Equal(0, prediction.LocalComparablesUsed);
            Assert.True(prediction.PricePerM2 > 0, "a missing location must not stop the valuation");
        }

        [Fact]
        public void PredictPricePerM2_NoFloorArea_Throws()
        {
            var listings = BuildMarket(400, garageWorth: 1.10, noiseScale: 0);

            var model = ValuationModel.Fit(listings);

            var subject = BuildSubject(hasGarage: false, beachMeters: 800);
            subject.AreaM2 = 0;

            Assert.Throws<ArgumentException>(() => model.PredictPricePerM2(subject));
        }

        [Fact]
        public void HasFeature_ReadsTheFieldBehindEachFeature()
        {
            var subject = BuildSubject(hasGarage: true, beachMeters: 500);
            subject.HasSeaView = true;
            subject.Condition = PropertyCondition.NewBuild;

            Assert.True(ValuationModel.HasFeature(subject, PremiumFeatures.HasGarage));
            Assert.True(ValuationModel.HasFeature(subject, PremiumFeatures.HasSeaView));
            Assert.True(ValuationModel.HasFeature(subject, PremiumFeatures.IsNewBuild));
            Assert.False(ValuationModel.HasFeature(subject, PremiumFeatures.HasSwimmingPool));

            // Beach proximity is a distance, not a yes/no feature - it has no field to read.
            Assert.False(ValuationModel.HasFeature(subject, PremiumFeatures.BeachProximity));
        }

        [Fact]
        public void HasFeature_NeedsRenovation_IsReadLikeAnyOtherYesNoFeature()
        {
            // Without this the valuation's "what the features contribute" list silently skipped
            // needing renovation, so a property in poor condition was priced down by the model
            // but never told why.
            var poor = BuildSubject(hasGarage: false, beachMeters: 500);
            poor.Condition = PropertyCondition.NeedsRenovation;

            var fine = BuildSubject(hasGarage: false, beachMeters: 500);
            fine.Condition = PropertyCondition.Good;

            Assert.True(ValuationModel.HasFeature(poor, PremiumFeatures.NeedsRenovation));
            Assert.False(ValuationModel.HasFeature(fine, PremiumFeatures.NeedsRenovation));

            // The quantities stay out of it - "does this property have a floor" is not a question.
            Assert.False(ValuationModel.HasFeature(poor, PremiumFeatures.FloorLevel));
            Assert.False(ValuationModel.HasFeature(poor, PremiumFeatures.EnergyGrade));
            Assert.False(ValuationModel.HasFeature(poor, PremiumFeatures.ExtraBathroom));
        }

        /// <summary>The letter for a position on the energy scale: 0 is G, 7 is A.</summary>
        private static string EnergyLetter(int score) => score switch
        {
            0 => "G", 1 => "F", 2 => "E", 3 => "D", 4 => "C", 5 => "B-", 6 => "B", _ => "A",
        };

        /// <summary>
        /// A property to price, matching the market <see cref="BuildMarket"/> builds.
        /// </summary>
        private static ValuationSubject BuildSubject(bool hasGarage, int beachMeters)
        {
            return new ValuationSubject
            {
                MarketAreaId = 1,
                Typology = Typology.T2,
                PropertyType = PropertyType.Apartment,
                Condition = PropertyCondition.Good,
                AreaM2 = 80,
                Bathrooms = 1,
                BalconyCount = 1,
                Floor = 2,
                ConstructionYear = 2000,
                DistanceToBeachMeters = beachMeters,
                Latitude = 37.08m,
                Longitude = -8.10m,
                HasGarage = hasGarage,
            };
        }

        /// <summary>
        /// Builds a synthetic market with a known answer baked in.
        ///
        /// Price per m² is <see cref="BasePricePerM2"/>, multiplied by
        /// <paramref name="garageWorth"/> when the listing has a garage, and by
        /// <paramref name="beachHalvingWorth"/> for each halving of the distance to the beach.
        /// Nothing else affects price. <paramref name="noiseScale"/> adds reproducible jitter
        /// (fixed seed) so tests about uncertainty have something to be uncertain about.
        ///
        /// Area, floor and beach distance vary across listings so no column sits perfectly
        /// constant - that would make several columns indistinguishable from the intercept.
        /// </summary>
        private static List<PropertyListing> BuildMarket(
            int count,
            double garageWorth,
            double noiseScale,
            double beachHalvingWorth = 1.0,
            double seaViewAt100mWorth = 1.0,
            bool seaViewDecaysWithDistance = false,
            double energyStepWorth = 1.0,
            double bathroomWorth = 1.0)
        {
            var random = new Random(20260810);

            // A second, independent stream for the attributes that are drawn rather than cycled.
            // Drawing them from `random` would shift the noise sequence and quietly move every
            // other test's numbers; keeping it separate leaves those byte-identical. Drawn, not
            // cycled, because i%3 and i%9 patterns line up exactly with the sea view and would
            // make two columns indistinguishable.
            var attributes = new Random(4242);

            var listings = new List<PropertyListing>();

            for (var i = 0; i < count; i++)
            {
                var hasGarage = i % 2 == 0;
                var areaM2 = 60 + i % 40;
                var beachMeters = 100 + i % 24 * 300;
                var hasSeaView = i % 3 == 0;

                var bathrooms = 1 + attributes.Next(3);
                var balconies = attributes.Next(2);
                var energyScore = attributes.Next(8);           // 0 = G through 7 = A
                var needsRenovation = attributes.Next(30) == 0;

                var halvingsFromOneKm = Math.Log(1000.0 / beachMeters) / Math.Log(2);

                // A sea view worth seaViewAt100mWorth at 100m, fading toward nothing as the
                // distance grows - the shape the production model fits with its interaction term.
                var seaViewMultiplier = 1.0;

                if (hasSeaView && seaViewAt100mWorth != 1.0)
                {
                    var fade = seaViewDecaysWithDistance
                        ? Math.Max(0, 1 - Math.Log(beachMeters / 100.0) / Math.Log(50))
                        : 1.0;

                    seaViewMultiplier = 1 + (seaViewAt100mWorth - 1) * fade;
                }

                var pricePerM2 = (double)BasePricePerM2
                    * (hasGarage ? garageWorth : 1.0)
                    * seaViewMultiplier
                    * Math.Pow(beachHalvingWorth, halvingsFromOneKm)
                    * Math.Pow(energyStepWorth, energyScore)
                    * Math.Pow(bathroomWorth, bathrooms)
                    * Math.Exp(noiseScale * (random.NextDouble() - 0.5));

                listings.Add(new PropertyListing
                {
                    Id = i + 1,
                    SourceUrl = $"https://example.test/{i + 1}",
                    MarketAreaId = 1,
                    PropertyType = PropertyType.Apartment,
                    Typology = Typology.T2,
                    Condition = needsRenovation ? PropertyCondition.NeedsRenovation : PropertyCondition.Good,
                    AreaM2 = areaM2,
                    Bathrooms = bathrooms,
                    BalconyCount = balconies,
                    EnergyCertificate = EnergyLetter(energyScore),
                    Floor = i % 5,
                    ConstructionYear = 1990 + i % 30,
                    DistanceToBeachMeters = beachMeters,
                    // Scattered over roughly a kilometre, so the neighbourhood correction has
                    // real neighbours to work with rather than a pile of identical points.
                    Latitude = 37.08m + i % 20 * 0.0005m,
                    Longitude = -8.10m + i % 17 * 0.0005m,
                    HasGarage = hasGarage,
                    HasSeaView = hasSeaView,
                    ListingSnapshots = new List<ListingSnapshot>
                    {
                        new()
                        {
                            Id = i + 1,
                            PropertyListingId = i + 1,
                            PricePerM2 = (decimal)pricePerM2,
                            Price = (decimal)pricePerM2 * areaM2,
                            SnapshotDateUtc = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc)
                        }
                    }
                });
            }

            return listings;
        }
    }
}
