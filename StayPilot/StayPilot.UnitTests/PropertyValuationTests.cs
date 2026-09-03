using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Helpers.Calculators;
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;

namespace StayPilot.UnitTests
{
    /// <summary>
    /// Everything about pricing ONE property: the fit, the prediction, the confidence, and the
    /// breakdown of which features the money is sitting in.
    ///
    /// Most of these build a synthetic market where the true answer is known by construction -
    /// a garage really is worth exactly 10%, and nothing else moves the price - then check the
    /// model recovers it. That is the only way to test a statistical model: against real data
    /// there is nothing to compare the output to. The exception is the robustness block, which
    /// pins the three ways this went wrong on REAL data, each of them invisible to a synthetic
    /// market (which has no broken rows, one location, and comps everywhere).
    ///
    /// What features are worth market-wide is a different question, tested in
    /// <see cref="FeaturePremiumCalculatorTests"/>.
    /// </summary>
    public class PropertyValuationTests
    {
        private const decimal BasePricePerM2 = 5000m;

        // ---------- Fitting ----------

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
        public void MarketAverages_DescribeTheListingsTheModelWasFittedOn()
        {
            // These exist so a valuation can say what "three bathrooms" is worth ABOVE the
            // typical property. If any of them came back zero the breakdown would measure
            // against nothing and pay every property a premium for being ordinary - so the
            // assertion that matters here is that they describe the market, not that they exist.
            var listings = BuildMarket(400, garageWorth: 1.10, noiseScale: 0);

            var model = ValuationModel.Fit(listings);

            // BuildMarket draws 1-3 bathrooms.
            Assert.InRange(model.MarketAverageBathrooms, 1.5, 2.5);

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

        // ---------- Predicting a price ----------

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
        public void PredictPricePerM2_CloserToTheBeach_IsStillPricedOnTheExactDistance()
        {
            // The REPORTED beach premium is now a plain "within 500m", but the price estimate
            // itself must keep using the smooth distance - otherwise two flats at 100m and 490m
            // would be priced identically, and a flat at 510m would fall off a cliff. This is
            // the test that says the simplification stopped at the reporting layer.
            var listings = BuildMarket(400, garageWorth: 1.0, noiseScale: 0, beachHalvingWorth: 1.08);

            var model = ValuationModel.Fit(listings);

            var onTheSand = model.PredictPricePerM2(BuildSubject(hasGarage: false, beachMeters: 100)).PricePerM2;
            var nearlyThere = model.PredictPricePerM2(BuildSubject(hasGarage: false, beachMeters: 490)).PricePerM2;
            var justOutside = model.PredictPricePerM2(BuildSubject(hasGarage: false, beachMeters: 510)).PricePerM2;

            // Inside the 500m band the price still varies with distance - no flat step.
            Assert.True(onTheSand > nearlyThere * 1.05m,
                $"100m ({onTheSand:F0}) should beat 490m ({nearlyThere:F0}) by more than rounding");

            // And crossing the threshold changes almost nothing, because the threshold does not
            // exist in the price model at all.
            Assert.InRange(justOutside, nearlyThere * 0.99m, nearlyThere * 1.0m);
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

        // ---------- Robustness against real data ----------

        [Fact]
        public void IsUsable_AreaTooSmallForItsTypology_IsRejected()
        {
            // The real shape of the fault: a genuine €349,000 asking price against an area the
            // scraper read as 2 m², which arrives as a perfectly ordinary-looking €174,500/m².
            var broken = ListingWith(typology: Typology.T3, areaM2: 2, pricePerM2: 174_500);

            Assert.False(ListingQuality.IsUsable(broken, broken.ListingSnapshots[0]));

            var real = ListingWith(typology: Typology.T3, areaM2: 90, pricePerM2: 3_800);

            Assert.True(ListingQuality.IsUsable(real, real.ListingSnapshots[0]));
        }

        [Fact]
        public void IsUsable_PricesOutsideTheRealMarket_AreRejectedBothWays()
        {
            var toocheap = ListingWith(Typology.T2, areaM2: 70, pricePerM2: 123);
            var tooDear = ListingWith(Typology.T2, areaM2: 70, pricePerM2: 60_000);

            Assert.False(ListingQuality.IsUsable(toocheap, toocheap.ListingSnapshots[0]));
            Assert.False(ListingQuality.IsUsable(tooDear, tooDear.ListingSnapshots[0]));

            // Genuinely expensive property still gets through - this rejects broken data, not
            // the top of the market.
            var prime = ListingWith(Typology.T2, areaM2: 70, pricePerM2: 11_000);

            Assert.True(ListingQuality.IsUsable(prime, prime.ListingSnapshots[0]));
        }

        [Fact]
        public void Fit_AFewWildlyMispricedListings_DoNotDragTheEstimate()
        {
            // Sabotage check for the second pass. These rows survive ListingQuality - the area
            // is plausible, the price is inside the market, and the row agrees with itself -
            // they are simply wrong by a factor of five. On squared error one of them carries
            // the weight of hundreds of ordinary rows, so without trimming they move the answer.
            var clean = FlatMarket(400, pricePerM2: 4_000);

            var polluted = FlatMarket(400, pricePerM2: 4_000);

            foreach (var listing in polluted.Take(8))
            {
                // Price moves with the rate. Changing only the rate makes the row disagree with
                // itself, and admission throws it out before the trimmer ever sees it - which
                // would test the wrong pass and pass for the wrong reason.
                listing.ListingSnapshots[0].PricePerM2 = 20_000;
                listing.ListingSnapshots[0].Price = 20_000 * listing.AreaM2;
            }

            var fromClean = ValuationModel.Fit(clean).PredictPricePerM2(FlatMarketSubject());
            var fromPolluted = ValuationModel.Fit(polluted).PredictPricePerM2(FlatMarketSubject());

            Assert.True(ValuationModel.Fit(polluted).DiscardedListings >= 8,
                "the eight sabotaged rows have to be the ones thrown out");

            var drift = Math.Abs(fromPolluted.PricePerM2 - fromClean.PricePerM2) / fromClean.PricePerM2;

            Assert.True(drift < 0.02m,
                $"eight bad rows in four hundred moved the estimate by {drift:P1}; it should barely notice");
        }

        // ---------- Admission: rows that cannot be describing a home ----------

        [Fact]
        public void IsUsable_RowWhosePricePerM2DisagreesWithPriceOverArea_IsRejected()
        {
            // Real shape, from the database: a Portimão advert asking EUR 123,123 over 91 m2 and
            // carrying EUR 123/m2. Every field is plausible alone; only the three together are
            // impossible, so every bound in isolation lets it through.
            var broken = ListingWith(Typology.T4, areaM2: 91, pricePerM2: 1_353m);

            // Deliberately inside the absolute price bounds, so the row can only be caught by
            // the three fields disagreeing - not by a floor that would have stopped it anyway.
            broken.ListingSnapshots[0].PricePerM2 = 5_000m;     // price and area left alone

            Assert.InRange(broken.ListingSnapshots[0].PricePerM2, 400m, 25_000m);
            Assert.False(ListingQuality.IsUsable(broken, broken.ListingSnapshots[0]));
        }

        [Fact]
        public void IsUsable_GrossVersusNetAreaRounding_IsStillAccepted()
        {
            // Adverts are loose about gross against net floor area, so the check has to tolerate
            // a few percent or it starts deleting sound rows.
            var listing = ListingWith(Typology.T2, areaM2: 70, pricePerM2: 4_000m);

            listing.ListingSnapshots[0].PricePerM2 = 4_120m;    // 3% out

            Assert.True(ListingQuality.IsUsable(listing, listing.ListingSnapshots[0]));
        }

        [Fact]
        public void UsableSubjects_ListingFarBelowItsOwnMunicipality_IsNotLearnedFrom()
        {
            // A 27 m2 studio asking EUR 13,500 where the município asks EUR 4,781/m2. Internally
            // consistent, structurally plausible, and not a flat - a timeshare week or a garage.
            // Only the local market can tell, which is why the absolute floor cannot catch it.
            var listings = FlatMarket(200, pricePerM2: 4_000);

            var notAHome = ListingWith(Typology.T0, areaM2: 27, pricePerM2: 500m);

            notAHome.Id = 90_001;
            notAHome.MarketAreaId = 1;
            notAHome.MarketArea = listings[0].MarketArea;
            notAHome.Latitude = listings[0].Latitude;
            notAHome.Longitude = listings[0].Longitude;

            listings.Add(notAHome);

            Assert.Equal(200, ListingQuality.UsableSubjects(listings, out _).Count);
        }

        [Fact]
        public void UsableSubjects_CheapButNotAbsurdListing_IsKept()
        {
            // Half the local rate is a bargain, which is the product's whole point. The floor
            // sits at a fifth precisely so it never reaches the stock a user wants to find.
            var listings = FlatMarket(200, pricePerM2: 4_000);

            var bargain = ListingWith(Typology.T2, areaM2: 70, pricePerM2: 2_000m);

            bargain.Id = 90_001;
            bargain.MarketAreaId = 1;
            bargain.MarketArea = listings[0].MarketArea;
            bargain.Latitude = listings[0].Latitude;
            bargain.Longitude = listings[0].Longitude;

            listings.Add(bargain);

            Assert.Equal(201, ListingQuality.UsableSubjects(listings, out _).Count);
        }

        [Fact]
        public void UsableSubjects_ThinMunicipality_HasNoFloorAppliedToIt()
        {
            // Too few listings to know what a place charges. Applying a floor anyway would delete
            // the thin markets first - the exact places least able to spare a row.
            var listings = FlatMarket(200, pricePerM2: 4_000);

            var thinPlace = Area(areaId: 2, district: "Bragança", municipality: "Vinhais",
                count: 5, pricePerM2: 3_000m);

            // One row at a seventh of its own handful of neighbours - under the fraction, but
            // still above the absolute floor, so only the local rule could reject it. Kept,
            // because five adverts cannot establish a median worth rejecting anything on.
            thinPlace[0].ListingSnapshots[0].PricePerM2 = 450m;
            thinPlace[0].ListingSnapshots[0].Price = 450m * thinPlace[0].AreaM2;

            listings.AddRange(thinPlace);

            Assert.Equal(205, ListingQuality.UsableSubjects(listings, out _).Count);
        }

        // ---------- Confidence reflects what the model knows, not just what is nearby ----------

        [Fact]
        public void Estimate_ThinDistrictWithCloseNeighbours_CannotClaimHighConfidence()
        {
            // The Portalegre case. Adverts cluster in towns, so ten comps inside a kilometre is
            // easy even where the model holds almost nothing for the surrounding district - and
            // distance alone read that as High.
            var dense = FlatMarket(400, pricePerM2: 4_000);

            // A second place, too small to earn a column of its own at any level, but with its
            // listings packed into one street so every distance test passes comfortably.
            // Twelve is under MinimumListingsPerArea, so it earns no column at area, município
            // or district level. Priced near the dense market on purpose: a place far enough
            // adrift gets trimmed as outliers and then has no neighbours to test with.
            var thin = Area(areaId: 2, district: "Portalegre", municipality: "Marvao",
                count: 12, pricePerM2: 3_200m);

            foreach (var listing in thin)
            {
                listing.Latitude = 39.39m + (listing.Id % 12 * 0.0001m);
                listing.Longitude = -7.37m + (listing.Id % 12 * 0.0001m);
            }

            var model = ValuationModel.Fit(dense.Concat(thin).ToList());

            var subject = ValuationSubject.FromListing(thin[0]);
            var prediction = model.PredictPricePerM2(subject);

            // The evidence around it would have earned High on distance and count alone.
            Assert.True(prediction.LocalComparablesUsed >= 10, "the thin place must have ten neighbours");
            Assert.True(prediction.NearestComparableMeters <= 1_000, "and they must be within a kilometre");
            Assert.Equal(LocationPrecision.National, prediction.LocationPrecision);

            // What the user is actually shown must not say High anyway.
            var owned = BuildOwned(floor: 1);

            owned.MarketAreaId = 2;
            owned.Latitude = thin[0].Latitude;
            owned.Longitude = thin[0].Longitude;

            var shown = PropertyValuation.Fit(dense.Concat(thin).ToList()).Estimate(owned, Array.Empty<FeatureEffect>());

            Assert.NotEqual(ValuationConfidence.High, shown.Confidence);
        }

        [Fact]
        public void Estimate_DensePlace_IsPricedAtItsOwnArea()
        {
            // The other side of the same rule: somewhere with real depth keeps its precision, so
            // the cap never costs a confident answer that was earned.
            var model = ValuationModel.Fit(FlatMarket(400, pricePerM2: 4_000));

            var prediction = model.PredictPricePerM2(FlatMarketSubject());

            Assert.Equal(LocationPrecision.Area, prediction.LocationPrecision);

            // And the ceiling costs it nothing - a dense place still reports High.
            var owned = BuildOwned(floor: 1);

            owned.Latitude = 37.08m;
            owned.Longitude = -8.10m;

            var shown = PropertyValuation.Fit(FlatMarket(400, pricePerM2: 4_000)).Estimate(owned, Array.Empty<FeatureEffect>());

            Assert.Equal(ValuationConfidence.High, shown.Confidence);
        }

        [Fact]
        public void Predict_AreaTooSparseToPriceOnItsOwn_IsPricedAsItsMunicipalityNotTheNation()
        {
            // Over half the market areas hold fewer than fifteen listings. They used to get no
            // column at all, which quietly priced them as whichever area happened to sort first
            // - so a cheap town could be valued off an expensive one on the other side of the
            // country. It should fall back to the municipality it actually sits in.
            var listings = new List<PropertyListing>();

            // The expensive municipality deliberately holds the LOWEST area id, because that is
            // the one the old code chose as its baseline - so a sparse zone with no column of
            // its own came out priced as beachfront Faro. Give the cheap side the low id and
            // this test passes either way and proves nothing.
            listings.AddRange(Area(areaId: 1, district: "Faro", municipality: "Rich", count: 200, pricePerM2: 8_000));

            // A cheap municipality: one well-covered area, plus thin zones that cannot stand
            // alone but together give the municipality enough to be priced on.
            listings.AddRange(Area(areaId: 50, district: "Beja", municipality: "Cheap", count: 100, pricePerM2: 1_200));
            listings.AddRange(Area(areaId: 99, district: "Beja", municipality: "Cheap", count: 5, pricePerM2: 1_200));
            listings.AddRange(Area(areaId: 98, district: "Beja", municipality: "Cheap", count: 5, pricePerM2: 1_200));
            listings.AddRange(Area(areaId: 97, district: "Beja", municipality: "Cheap", count: 6, pricePerM2: 1_200));

            var model = ValuationModel.Fit(listings);

            // No coordinates, so the neighbourhood correction cannot rescue it - this measures
            // the location column alone.
            var subject = FlatMarketSubject();
            subject.MarketAreaId = 99;
            subject.District = "Beja";
            subject.Municipality = "Cheap";
            subject.Latitude = null;
            subject.Longitude = null;

            var predicted = model.PredictPricePerM2(subject).PricePerM2;

            Assert.InRange(predicted, 900m, 1_600m);
        }

        [Fact]
        public void Predict_NothingNearby_QuotesAWiderRangeThanAPropertySurroundedByComps()
        {
            // Both used to be handed the same range, which made the stranded one look exactly as
            // well-evidenced as the one with a street full of comps.
            var model = ValuationModel.Fit(FlatMarket(400, pricePerM2: 4_000));

            var surrounded = FlatMarketSubject();

            var stranded = FlatMarketSubject();
            stranded.Latitude = 41.15m;              // Porto; every listing here is in the Algarve
            stranded.Longitude = -8.61m;

            var withComps = model.PredictPricePerM2(surrounded);
            var withNone = model.PredictPricePerM2(stranded);

            Assert.Equal(0, withNone.LocalComparablesUsed);
            Assert.True(withNone.Spread > withComps.Spread * 1.5,
                $"a stranded property must be quoted far less precisely; got {withNone.Spread:F3} vs {withComps.Spread:F3}");
        }

        [Fact]
        public void Estimate_NothingNearby_ComesBackWithLowConfidenceAndAWiderPriceRange()
        {
            // The same thing again, but through the front door - this is the shape the API
            // actually returns, and the range has to widen there too, not just in the model.
            var valuation = PropertyValuation.Fit(FlatMarket(400, pricePerM2: 4_000));

            var local = BuildOwned(floor: 2);
            local.Latitude = 37.08m;
            local.Longitude = -8.10m;
            local.MarketAreaId = 1;

            var stranded = BuildOwned(floor: 2);
            stranded.Latitude = 41.15m;
            stranded.Longitude = -8.61m;
            stranded.MarketAreaId = 1;

            var near = valuation.Estimate(local, Array.Empty<FeatureEffect>());
            var far = valuation.Estimate(stranded, Array.Empty<FeatureEffect>());

            Assert.Equal(ValuationConfidence.Low, far.Confidence);

            var nearWidth = (near.MaxPrice - near.MinPrice) / near.MidPrice;
            var farWidth = (far.MaxPrice - far.MinPrice) / far.MidPrice;

            Assert.True(farWidth > nearWidth,
                $"a stranded property must be quoted more widely; got {farWidth:P0} vs {nearWidth:P0}");
        }

        // ---------- Which features the money is sitting in ----------

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

            // Close-to-beach is answered from the distance, not from a flag, so it has no field
            // here to read - PropertyValuation.Carries handles it.
            Assert.False(ValuationModel.HasFeature(subject, PremiumFeatures.CloseToBeach));
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

        [Fact]
        public void Estimate_LiftHighUp_IsCreditedTheHighFloorFigureNotTheAverage()
        {
            // The whole point of measuring the lift in two tiers: a fourth-floor flat has to be
            // paid for the lift it actually has, not for the average of one it has and one a
            // ground-floor flat has.
            var high = Estimate(BuildOwned(floor: 4), LiftEffect());
            var low = Estimate(BuildOwned(floor: 0), LiftEffect());

            var highRow = high.Adjustments.Single();
            var lowRow = low.Adjustments.Single();

            Assert.True(highRow.Amount > lowRow.Amount,
                $"a lift on the 4th floor came back at {highRow.Amount} against {lowRow.Amount} on the ground");

            // The 6% figure, not the 2% headline: price * (1 - 1/1.06).
            AssertAmountIs(high.MidPrice, percent: 6m, highRow.Amount);
            AssertAmountIs(low.MidPrice, percent: 2m, lowRow.Amount);

            // The conditions have to travel with the figure, or the breakdown claims more than
            // it measured.
            Assert.Equal("on the 3rd floor or above", highRow.Detail);
            Assert.Null(lowRow.Detail);
        }

        [Fact]
        public void Estimate_LiftOnAFlatThatNeverStatedItsFloor_IsCreditedTheOrdinaryFigure()
        {
            // No stated floor is not a fourth floor. Reaching for the better number here would
            // pay a premium to every listing that simply left the field blank.
            var estimate = Estimate(BuildOwned(floor: null), LiftEffect());

            var row = estimate.Adjustments.Single();

            AssertAmountIs(estimate.MidPrice, percent: 2m, row.Amount);
            Assert.Null(row.Detail);
        }

        [Fact]
        public void Estimate_ParkingOnAFlatWithAGarage_EarnsNoSecondRow()
        {
            // The garage row already covers somewhere to put the car. Both rows would charge the
            // buyer twice for one parking space.
            var owned = BuildOwned(floor: 1);
            owned.HasElevator = false;
            owned.HasGarage = true;
            owned.HasParking = true;

            var labels = Estimate(owned,
                new FeatureEffect { Feature = PremiumFeatures.HasGarage, Percent = 10m, LowerPercent = 8m, UpperPercent = 12m },
                new FeatureEffect { Feature = PremiumFeatures.HasParking, Percent = 3m, LowerPercent = 1m, UpperPercent = 5m })
                .Adjustments.Select(x => x.Label).ToList();

            Assert.Contains("Garage", labels);
            Assert.DoesNotContain("Parking", labels);
        }

        [Fact]
        public void Estimate_BalconyOnAFlatWithATerrace_EarnsNoSecondRow()
        {
            // Same reasoning as parking, and the same reason the balcony's premium is measured
            // on the flats with no terrace: one piece of outdoor space, one row.
            var owned = BuildOwned(floor: 1);
            owned.HasElevator = false;
            owned.HasTerrace = true;
            owned.BalconyCount = 1;

            var labels = Estimate(owned,
                new FeatureEffect { Feature = PremiumFeatures.HasTerrace, Percent = 5m, LowerPercent = 4m, UpperPercent = 6m },
                new FeatureEffect { Feature = PremiumFeatures.HasBalcony, Percent = 2m, LowerPercent = 1m, UpperPercent = 3m })
                .Adjustments.Select(x => x.Label).ToList();

            Assert.Contains("Terrace", labels);
            Assert.DoesNotContain("Balcony", labels);
        }

        // ---------- Features that cost a property money ----------

        [Fact]
        public void Estimate_MeasurableNeedsRenovation_ShowsWhatTheWorkCostsRatherThanZero()
        {
            // The clamp this replaces valued a flat needing work exactly as though the work were
            // free - in a product whose headline analytic is renovation upside. A premium that is
            // negative by nature, and measurable, is the finding.
            var owned = BuildOwned(floor: 1);
            owned.HasElevator = false;
            owned.Condition = PropertyCondition.NeedsRenovation;

            var estimate = Estimate(owned, new FeatureEffect
            {
                Feature = PremiumFeatures.NeedsRenovation,
                Percent = -18m,
                LowerPercent = -24m,        // range stays entirely below zero, so this is measurable
                UpperPercent = -12m,
            });

            var renovation = Assert.Single(estimate.Adjustments, x => x.Label.Contains("enovation"));

            Assert.True(renovation.IsMeasurable);
            Assert.True(renovation.Amount < 0, $"expected a negative amount, got {renovation.Amount}");
            AssertAmountIs(estimate.MidPrice, -18m, renovation.Amount);
        }

        [Fact]
        public void Estimate_UnmeasurableNegativePremium_IsStillFlooredAtZero()
        {
            // A feature that ought to be positive and came back negative out of noise - a garage
            // at -2% on a range straddling zero. Passing that through would bill the owner for
            // having a garage, which is the reason the floor existed in the first place.
            var owned = BuildOwned(floor: 1);
            owned.HasElevator = false;
            owned.HasGarage = true;

            var estimate = Estimate(owned, new FeatureEffect
            {
                Feature = PremiumFeatures.HasGarage,
                Percent = -2m,
                LowerPercent = -9m,         // straddles zero, so the effect is not a finding
                UpperPercent = 5m,
            });

            var garage = Assert.Single(estimate.Adjustments, x => x.Label.Contains("arage"));

            Assert.False(garage.IsMeasurable);
            Assert.Equal(0m, garage.Amount);
        }

        [Theory]
        [InlineData(100, 1.00)]         // on the sand: the whole premium
        [InlineData(500, 1.00)]         // exactly on the line still counts as close
        [InlineData(1_250, 0.50)]       // halfway from 500m to 2km: half of it
        [InlineData(1_625, 0.25)]
        [InlineData(2_000, 0.00)]       // 2km: nothing left
        [InlineData(5_000, 0.00)]
        public void Estimate_CloseToBeach_FullPremiumInsideTheBandThenFadesToNothingAtTwoKm(
            int beachMeters, double expectedShare)
        {
            var estimate = EstimateAtBeachDistance(beachMeters);

            var row = estimate.Adjustments.SingleOrDefault(x => x.Label == "Close To Beach");

            if (expectedShare == 0)
            {
                Assert.Null(row);

                return;
            }

            Assert.NotNull(row);

            // The share is applied as a fraction of a unit, so it compounds the same way a
            // fractional bathroom would: price * (1 - 1 / 1.09^share).
            AssertAmountIs(estimate.MidPrice, percent: 9m, row!.Amount, units: (decimal)expectedShare);
        }

        [Fact]
        public void Estimate_CloseToBeach_HasNoCliffAtTheThreshold()
        {
            // The reason the fade exists at all. One metre is not the difference between a beach
            // flat and an inland one, so 501m has to come back worth almost exactly what 500m is.
            var justInside = BeachRowAmount(500);
            var justOutside = BeachRowAmount(501);

            Assert.NotNull(justOutside);
            Assert.InRange(justOutside!.Value, justInside!.Value * 0.99m, justInside.Value);
        }

        [Fact]
        public void Estimate_CloseToBeach_ShrinksTheFurtherOutTheFlatIs()
        {
            var distances = new[] { 100, 600, 1_000, 1_400, 1_800 };

            var amounts = distances.Select(x => BeachRowAmount(x)!.Value).ToList();

            foreach (var (nearer, further) in amounts.Zip(amounts.Skip(1)))
            {
                Assert.True(nearer > further,
                    $"the credit must shrink with distance; got {nearer} then {further}");
            }
        }

        [Fact]
        public void Estimate_CloseToBeach_DistanceNeverRecorded_EarnsNoRow()
        {
            // "We did not measure it" is not evidence of being anywhere near the sea.
            var owned = BuildOwned(floor: 1);
            owned.HasElevator = false;
            owned.DistanceToBeachMeters = null;

            var estimate = Estimate(owned, CloseToBeachEffect());

            Assert.Empty(estimate.Adjustments);
        }

        [Fact]
        public void Estimate_CloseToBeach_PartCreditedRow_SaysSoOnTheRow()
        {
            // A reader seeing less than the headline 9% is owed the reason on the row itself.
            var full = EstimateAtBeachDistance(300).Adjustments.Single();
            var partial = EstimateAtBeachDistance(1_250).Adjustments.Single();

            // Number formatting follows the machine's culture, so only the wording is asserted.
            Assert.Equal("300m from the beach", full.Detail);
            Assert.DoesNotContain("of the premium", full.Detail);

            Assert.Contains("from the beach", partial.Detail);
            Assert.Contains("of the premium", partial.Detail);
            Assert.Contains("50", partial.Detail);
        }

        [Fact]
        public void Estimate_SeaViewOnTheBeachfront_IsCreditedTheBeachfrontFigure()
        {
            // The sea view's conditional figure hangs off the SAME 500m the Close to Beach row
            // uses. Two thresholds that meant "close to the beach" would eventually drift apart.
            var seaView = new FeatureEffect
            {
                Feature = PremiumFeatures.HasSeaView,
                Percent = 8m,
                LowerPercent = 7m,
                UpperPercent = 9m,
                MaximumPercent = 14m,
                MaximumBasis = $"within {ValuationSubject.CloseToBeachMeters}m of the beach",
            };

            var beachfront = BuildOwned(floor: 1);
            beachfront.HasElevator = false;
            beachfront.HasSeaView = true;
            beachfront.DistanceToBeachMeters = 200;

            var inland = BuildOwned(floor: 1);
            inland.HasElevator = false;
            inland.HasSeaView = true;
            inland.DistanceToBeachMeters = 3_000;

            var near = Estimate(beachfront, seaView);
            var far = Estimate(inland, seaView);

            AssertAmountIs(near.MidPrice, percent: 14m, near.Adjustments.Single().Amount);
            AssertAmountIs(far.MidPrice, percent: 8m, far.Adjustments.Single().Amount);

            Assert.NotNull(near.Adjustments.Single().Detail);
            Assert.Null(far.Adjustments.Single().Detail);
        }

        [Fact]
        public void Estimate_FeatureTheFlatDoesNotHave_IsNeverReportedAsADiscount()
        {
            // A flat with no garage is priced as a flat with no garage. Billing it for the
            // missing garage here would charge for the same absence twice.
            var owned = BuildOwned(floor: 1);
            owned.HasElevator = false;
            owned.HasGarage = false;

            var estimate = Estimate(owned,
                new FeatureEffect { Feature = PremiumFeatures.HasGarage, Percent = 10m, LowerPercent = 8m, UpperPercent = 12m });

            Assert.Empty(estimate.Adjustments);
        }

        // ---------- Where the property actually is ----------

        [Fact]
        public void Predict_ZonePickedWrong_PricesTheZoneTheCoordinatesAreIn()
        {
            // The zone arrives from a dropdown; the coordinates cannot be mis-picked. On real data
            // the same Quarteira flat priced as the cheap old town and as the town-wide catch-all
            // zone came out 30% apart, decided entirely by which one the picker happened to say -
            // while the coordinates, which knew, were only trimming the answer by a few percent.
            var listings = new List<PropertyListing>();

            listings.AddRange(AreaAt(areaId: 1, "Faro", "Loule", 200, 8_000m, 37.08m, -8.10m));
            listings.AddRange(AreaAt(areaId: 2, "Faro", "Loule", 200, 3_000m, 37.20m, -8.30m));

            var model = ValuationModel.Fit(listings);

            var subject = FlatMarketSubject();
            subject.MarketAreaId = 1;        // the expensive zone, chosen by mistake
            subject.Latitude = 37.20m;       // standing in the cheap one
            subject.Longitude = -8.30m;

            var prediction = model.PredictPricePerM2(subject);

            Assert.Equal(2, prediction.LocatedMarketAreaId);
            Assert.True(prediction.LocatedByCoordinates);

            // Priced as the cheap zone it is in, not the expensive one it was filed under.
            Assert.InRange(prediction.PricePerM2, 2_400m, 3_600m);
        }

        [Fact]
        public void Predict_PropertyWithNoCoordinates_KeepsTheZoneItWasFiledUnder()
        {
            // Nothing to override the picker with, so the picker is the best answer we have.
            var model = ValuationModel.Fit(FlatMarket(400, pricePerM2: 4_000));

            var subject = FlatMarketSubject();
            subject.Latitude = null;
            subject.Longitude = null;

            var prediction = model.PredictPricePerM2(subject);

            Assert.Equal(subject.MarketAreaId, prediction.LocatedMarketAreaId);
            Assert.False(prediction.LocatedByCoordinates);
        }

        [Fact]
        public void Predict_NoListingCloseEnoughToVote_KeepsTheZoneItWasFiledUnder()
        {
            // A property outside the collected area has coordinates, but they are not evidence of
            // being in anyone's zone - borrowing the nearest zone from 300km away would be worse
            // than believing the address.
            var model = ValuationModel.Fit(FlatMarket(400, pricePerM2: 4_000));

            var subject = FlatMarketSubject();
            subject.Latitude = 41.15m;       // Porto; every listing here is in the Algarve
            subject.Longitude = -8.61m;

            var prediction = model.PredictPricePerM2(subject);

            Assert.Equal(subject.MarketAreaId, prediction.LocatedMarketAreaId);
            Assert.False(prediction.LocatedByCoordinates);
        }

        [Fact]
        public void Predict_CoordinatesLandInAZoneNamedAfterItsTown_IsPricedAsARealZoneInstead()
        {
            // The source files a listing under a zone named after the whole town whenever it does
            // not know the neighbourhood, so "Quarteira / Quarteira" holds stock from every corner
            // of Quarteira and overlaps all 23 real zones in it. It usually has more listings in a
            // town centre than any real zone, so left votable it won nearly every vote - which
            // swapped one mis-picked zone for a worse one and cost a real flat 30%.
            var realZone = AreaAt(areaId: 1, "Faro", "Loule", 200, 3_000m, 37.08m, -8.10m);
            var catchAll = AreaAt(areaId: 2, "Faro", "Loule", 200, 9_000m, 37.08m, -8.10m);

            foreach (var listing in realZone)
            {
                listing.MarketArea!.Town = "Quarteira";
                listing.MarketArea.Zone = "Centro";
            }

            // Same coordinates as the real zone, because that is the whole problem: it is not
            // somewhere, it is everywhere in the town.
            foreach (var listing in catchAll)
            {
                listing.MarketArea!.Town = "Quarteira";
                listing.MarketArea.Zone = "Quarteira";
            }

            var model = ValuationModel.Fit(realZone.Concat(catchAll).ToList());

            var subject = FlatMarketSubject();
            subject.MarketAreaId = 2;        // filed under the catch-all
            subject.Latitude = 37.08m;
            subject.Longitude = -8.10m;

            var prediction = model.PredictPricePerM2(subject);

            Assert.Equal(1, prediction.LocatedMarketAreaId);
            Assert.True(prediction.LocatedByCoordinates);
        }

        [Fact]
        public void CatchAllAreasIn_TownWithNoOtherZone_IsTakenAtItsWord()
        {
            // Where the town-named zone is the only one we have listings for, it is the most
            // specific thing we know - discarding it would price the town off its municipality
            // for no reason.
            var onlyZone = AreaAt(areaId: 1, "Faro", "Loule", 200, 3_000m, 37.08m, -8.10m);

            foreach (var listing in onlyZone)
            {
                listing.MarketArea!.Town = "Quarteira";
                listing.MarketArea.Zone = "Quarteira";
            }

            var subjects = ListingQuality.UsableSubjects(onlyZone).Select(x => x.Subject).ToList();

            Assert.Empty(ValuationModel.CatchAllAreasIn(subjects));
        }

        // ---------- The same flat, advertised twice ----------

        [Fact]
        public void UsableSubjects_SameFlatAdvertisedTwice_IsCountedOnce()
        {
            // Agencies re-advertise each other's stock. Counted twice, a copy pulls the fit toward
            // whatever it is, and the ten "nearest neighbours" can be one advert ten times.
            var listings = FlatMarket(200, pricePerM2: 4_000);

            listings.Add(CopyOfTheSameFlat(listings[0], listingId: 90_001));

            Assert.Equal(200, ListingQuality.UsableSubjects(listings, out var collapsed).Count);
            Assert.Equal(1, collapsed);
        }

        [Fact]
        public void UsableSubjects_UnitsOfOneDevelopmentAtTheSamePrice_AreAllKept()
        {
            // A development advertising its units shares a price and a floor area but not a
            // position, and those are real separate flats. On this data 1,625 of 1,823 same-price
            // groups are that rather than re-advertisements, so a key without the coordinates
            // would delete most of a development every time.
            var listings = FlatMarket(200, pricePerM2: 4_000);

            var firstUnit = CopyOfTheSameFlat(listings[0], listingId: 90_001);
            var secondUnit = CopyOfTheSameFlat(listings[0], listingId: 90_002);

            firstUnit.Latitude = 37.15m;
            firstUnit.Longitude = -8.15m;
            secondUnit.Latitude = 37.15m;
            secondUnit.Longitude = -8.1501m;     // next door, same asking price

            listings.Add(firstUnit);
            listings.Add(secondUnit);

            Assert.Equal(202, ListingQuality.UsableSubjects(listings, out var collapsed).Count);
            Assert.Equal(0, collapsed);
        }

        [Fact]
        public void UsableSubjects_SameFlatRelistedAndRegeocoded_IsCountedOnce()
        {
            // The duplicate that actually occurs in this database. Both adverts carry the agency's
            // own reference, but the re-listing was geocoded 300m away and re-priced, so the exact
            // key sees two different flats. Measured on real data, this shape is 282 of the 293
            // duplicate groups - the exact key alone caught 11.
            var listings = FlatMarket(200, pricePerM2: 4_000);

            var relisted = CopyOfTheSameFlat(listings[0], listingId: 90_001);

            listings[0].Notes = "Ref: 26960236 | Area basis: bruta";
            relisted.Notes = "Ref: 26960236 | Area basis: bruta | coords approximate";
            relisted.Latitude = listings[0].Latitude + 0.0027m;          // ~300m north
            relisted.ListingSnapshots.First().Price += 5_000m;           // re-listed slightly dearer

            listings.Add(relisted);

            Assert.Equal(200, ListingQuality.UsableSubjects(listings, out var collapsed).Count);
            Assert.Equal(1, collapsed);
        }

        [Fact]
        public void UsableSubjects_SameReferenceInTwoDifferentTowns_IsAReferenceCollisionAndBothAreKept()
        {
            // An agency reference is its own filing number, not a portal id, so short ones repeat
            // across agencies: on this data 447 same-reference groups are a Faro flat and a
            // Setúbal one. Distance is what tells a collision from a re-advertisement.
            var listings = FlatMarket(200, pricePerM2: 4_000);

            var elsewhere = CopyOfTheSameFlat(listings[0], listingId: 90_001);

            listings[0].Notes = "Ref: 002 | Area basis: bruta";
            elsewhere.Notes = "Ref: 002 | Area basis: bruta";
            elsewhere.Latitude = listings[0].Latitude + 1.0m;            // a different district entirely

            listings.Add(elsewhere);

            Assert.Equal(201, ListingQuality.UsableSubjects(listings, out var collapsed).Count);
            Assert.Equal(0, collapsed);
        }

        [Fact]
        public void UsableSubjects_DevelopmentUnitsUnderTheirOwnReferences_AreAllKept()
        {
            // A reference identifies a unit, so a development's flats carry distinct ones even
            // when they share an address, a size and a price. This is the case the second pass
            // must not eat - the units are real separate flats and real market evidence.
            var listings = FlatMarket(200, pricePerM2: 4_000);

            var unitA = CopyOfTheSameFlat(listings[0], listingId: 90_001);
            var unitB = CopyOfTheSameFlat(listings[0], listingId: 90_002);

            listings[0].Notes = "Ref: 123891235-26 | Area basis: bruta";
            unitA.Notes = "Ref: 123891235-27 | Area basis: bruta";
            unitB.Notes = "Ref: 123891235-28 | Area basis: bruta";

            // Same building, so the geocoder puts them within metres of each other.
            unitA.Latitude = listings[0].Latitude + 0.00002m;
            unitB.Latitude = listings[0].Latitude + 0.00004m;

            listings.Add(unitA);
            listings.Add(unitB);

            Assert.Equal(202, ListingQuality.UsableSubjects(listings, out var collapsed).Count);
            Assert.Equal(0, collapsed);
        }

        [Fact]
        public void UsableSubjects_ListingsWithoutAReference_AreNeverMergedOnDistanceAlone()
        {
            // No reference is no evidence of identity. Two neighbouring flats of the same size
            // must survive, or the second pass becomes a proximity filter that deletes a street.
            var listings = FlatMarket(200, pricePerM2: 4_000);

            var neighbour = CopyOfTheSameFlat(listings[0], listingId: 90_001);

            listings[0].Notes = null;
            neighbour.Notes = "Area basis: bruta";                       // notes, but no Ref: prefix
            neighbour.Latitude = listings[0].Latitude + 0.0001m;
            neighbour.ListingSnapshots.First().Price += 1_000m;

            listings.Add(neighbour);

            Assert.Equal(201, ListingQuality.UsableSubjects(listings, out var collapsed).Count);
            Assert.Equal(0, collapsed);
        }

        // ---------- Helpers ----------

        /// <summary>
        /// The same property re-advertised: every field a duplicate would share, under a new id.
        /// </summary>
        private static PropertyListing CopyOfTheSameFlat(PropertyListing original, int listingId)
        {
            var snapshot = original.ListingSnapshots.First();

            return new PropertyListing
            {
                Id = listingId,
                MarketAreaId = original.MarketAreaId,
                MarketArea = original.MarketArea,
                Typology = original.Typology,
                PropertyType = original.PropertyType,
                Condition = original.Condition,
                AreaM2 = original.AreaM2,
                Bathrooms = original.Bathrooms,
                Latitude = original.Latitude,
                Longitude = original.Longitude,
                ListingSnapshots = new List<ListingSnapshot>
                {
                    new()
                    {
                        PricePerM2 = snapshot.PricePerM2,
                        Price = snapshot.Price,
                        SnapshotDateUtc = snapshot.SnapshotDateUtc,
                    },
                },
            };
        }

        /// <summary>
        /// The same market <see cref="Area"/> builds, moved somewhere of its own. The shared
        /// builder stacks every area on one spot, which is fine while only the location column is
        /// under test and useless once the coordinates are the thing being tested.
        /// </summary>
        private static List<PropertyListing> AreaAt(
            int areaId, string district, string municipality, int count, decimal pricePerM2,
            decimal latitude, decimal longitude)
        {
            var listings = Area(areaId, district, municipality, count, pricePerM2);

            for (var i = 0; i < listings.Count; i++)
            {
                // Roughly 20m a step, so the area covers a few hundred metres and every listing
                // in it is close enough to vote on which zone a property standing there is in.
                listings[i].Latitude = latitude + (i % 20 * 0.0002m);
                listings[i].Longitude = longitude + (i % 17 * 0.0002m);
            }

            return listings;
        }

        /// <summary>
        /// The amount a feature worth <paramref name="percent"/> should account for: what
        /// disappears when its multiplier is divided back out. <paramref name="units"/> is how
        /// much of the feature the property earns - one whole garage, or the shrinking share of
        /// the beach premium a flat 1,250m out is entitled to.
        /// </summary>
        private static void AssertAmountIs(decimal midPrice, decimal percent, decimal actual, decimal units = 1m)
        {
            var multiplier = (decimal)Math.Pow(1 + (double)percent / 100, (double)units);
            var expected = midPrice * (1 - 1 / multiplier);

            Assert.InRange(actual, expected - 1m, expected + 1m);
        }

        /// <summary>An otherwise plain flat, sitting <paramref name="beachMeters"/> from the sea.</summary>
        private static PropertyEstimate EstimateAtBeachDistance(int beachMeters)
        {
            var owned = BuildOwned(floor: 1);

            owned.HasElevator = false;
            owned.DistanceToBeachMeters = beachMeters;

            return Estimate(owned, CloseToBeachEffect());
        }

        /// <summary>What the beach row came to, or null when there was no beach row.</summary>
        private static decimal? BeachRowAmount(int beachMeters)
        {
            return EstimateAtBeachDistance(beachMeters).Adjustments.SingleOrDefault()?.Amount;
        }

        /// <summary>A lift worth 2% ordinarily and 6% from the third floor up.</summary>
        private static FeatureEffect LiftEffect()
        {
            return new FeatureEffect
            {
                Feature = PremiumFeatures.HasElevator,
                Percent = 2m,
                LowerPercent = 1m,
                UpperPercent = 3m,
                MaximumPercent = 6m,
                MaximumBasis = "on the 3rd floor or above",
            };
        }

        /// <summary>Being within 500m of the sea, worth 9%.</summary>
        private static FeatureEffect CloseToBeachEffect()
        {
            return new FeatureEffect
            {
                Feature = PremiumFeatures.CloseToBeach,
                Percent = 9m,
                LowerPercent = 7m,
                UpperPercent = 11m,
                Basis = $"within {ValuationSubject.CloseToBeachMeters}m of the beach",
            };
        }

        /// <summary>
        /// One property valued against handed-in premiums. The premiums are handed in rather
        /// than measured so the assertions are about the crediting, not about whatever a
        /// synthetic market happened to be worth that day.
        /// </summary>
        private static PropertyEstimate Estimate(OwnedPropertyResponse owned, params FeatureEffect[] effects)
        {
            var valuation = PropertyValuation.Fit(BuildMarket(400, garageWorth: 1.10, noiseScale: 0));

            return valuation.Estimate(owned, effects);
        }

        /// <summary>A flat with a lift on <paramref name="floor"/> and nothing else of note.</summary>
        private static OwnedPropertyResponse BuildOwned(int? floor)
        {
            return new OwnedPropertyResponse
            {
                MarketAreaId = 1,
                PropertyType = PropertyType.Apartment,
                Typology = Typology.T2,
                AreaM2 = 80,
                Bathrooms = 1,
                Floor = floor,
                HasElevator = true,
                Condition = PropertyCondition.Good,
            };
        }

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

        /// <summary>A property matching <see cref="FlatMarket"/>, sitting in its middle.</summary>
        private static ValuationSubject FlatMarketSubject()
        {
            var subject = BuildSubject(hasGarage: false, beachMeters: 800);

            subject.District = "Faro";
            subject.Municipality = "Loule";

            return subject;
        }

        /// <summary>
        /// One market, one price, with just enough variation in the other fields that no column
        /// sits perfectly constant and becomes indistinguishable from the intercept.
        /// </summary>
        private static List<PropertyListing> FlatMarket(int count, decimal pricePerM2)
        {
            return Area(areaId: 1, district: "Faro", municipality: "Loule", count: count, pricePerM2: pricePerM2);
        }

        private static List<PropertyListing> Area(
            int areaId, string district, string municipality, int count, decimal pricePerM2)
        {
            var listings = new List<PropertyListing>();

            for (var i = 0; i < count; i++)
            {
                var listing = ListingWith(Typology.T2, 60 + (i % 40), pricePerM2);

                listing.Id = (areaId * 10_000) + i;
                listing.MarketAreaId = areaId;
                listing.MarketArea = new MarketArea { Id = areaId, District = district, Municipality = municipality };
                listing.Floor = i % 5;
                listing.ConstructionYear = 1990 + (i % 30);
                listing.DistanceToBeachMeters = 100 + (i % 24 * 300);
                listing.HasGarage = i % 2 == 0;

                // Spread over roughly a kilometre so the neighbourhood correction has real
                // neighbours rather than a pile of identical points.
                listing.Latitude = 37.08m + (i % 20 * 0.0005m);
                listing.Longitude = -8.10m + (i % 17 * 0.0005m);

                listings.Add(listing);
            }

            return listings;
        }

        private static PropertyListing ListingWith(Typology typology, int areaM2, decimal pricePerM2)
        {
            return new PropertyListing
            {
                Id = 1,
                MarketAreaId = 1,
                Typology = typology,
                PropertyType = PropertyType.Apartment,
                Condition = PropertyCondition.Good,
                AreaM2 = areaM2,
                Bathrooms = 1,
                ListingSnapshots = new List<ListingSnapshot>
                {
                    new()
                    {
                        PricePerM2 = pricePerM2,
                        Price = pricePerM2 * areaM2,
                        SnapshotDateUtc = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
                    },
                },
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
