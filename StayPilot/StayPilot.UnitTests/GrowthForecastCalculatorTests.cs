using StayPilot.Application.Helpers.Calculators;
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;

namespace StayPilot.UnitTests
{
    // The forecast, and the two rates it is made of.
    //
    // What most of these defend is that the seeded assumption and the measured trend never merge
    // into one unattributable number: a projection that cannot be taken apart cannot be argued
    // with, and this one is built on an assumption that deserves arguing with.
    public class GrowthForecastCalculatorTests
    {
        private static readonly DateTime Now = new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc);

        private static HousePriceGrowth Seeded(decimal annual = 6m, decimal volatility = 3m)
        {
            return new HousePriceGrowth
            {
                District = "Faro",
                AnnualGrowthPercent = annual,
                VolatilityPercentagePoints = volatility,
                Source = "test assumption",
                AsOfYear = 2026,
            };
        }

        // A place whose median price per m2 moves by monthlyStep euros each month, observed for
        // "months" months with "perMonth" adverts priced in each.
        private static List<PropertyListing> Series(int months, int perMonth, decimal startPricePerM2, decimal monthlyStep)
        {
            var listings = new List<PropertyListing>();

            for (var month = 0; month < months; month++)
            {
                var snapshots = new List<ListingSnapshot>();

                for (var i = 0; i < perMonth; i++)
                {
                    snapshots.Add(new ListingSnapshot
                    {
                        // Mid-month, so a month never bleeds into its neighbour.
                        SnapshotDateUtc = Now.AddMonths(-(months - 1 - month)).AddDays(-14),
                        PricePerM2 = startPricePerM2 + (monthlyStep * month),
                        Price = (startPricePerM2 + (monthlyStep * month)) * 80m,
                        Status = ListingStatus.Active,
                    });
                }

                listings.Add(new PropertyListing
                {
                    CreatedAtUtc = Now.AddMonths(-months),
                    ListingSnapshots = snapshots,
                });
            }

            return listings;
        }

        [Fact]
        public void MeasureLocalTrend_TooFewSnapshots_ReportsNoRate()
        {
            var listings = Series(months: 6, perMonth: 2, startPricePerM2: 3000m, monthlyStep: 10m);

            var trend = GrowthForecastCalculator.MeasureLocalTrend(listings, Now);

            Assert.Null(trend.AnnualPercent);
            Assert.Contains("at least", trend.Reason);
        }

        [Fact]
        public void MeasureLocalTrend_AllSnapshotsInOneWeek_ReportsNoDirection()
        {
            // The state this database is actually in early on: plenty of prices, no time between them.
            var listings = Enumerable.Range(0, 20).Select(i => new PropertyListing
            {
                CreatedAtUtc = Now.AddDays(-3),
                ListingSnapshots = Enumerable.Range(0, 5).Select(s => new ListingSnapshot
                {
                    SnapshotDateUtc = Now.AddDays(-(s % 3)),
                    PricePerM2 = 3000m + i,
                    Price = 240000m,
                    Status = ListingStatus.Active,
                }).ToList(),
            }).ToList();

            var trend = GrowthForecastCalculator.MeasureLocalTrend(listings, Now);

            Assert.Null(trend.AnnualPercent);
            Assert.Contains("too short to have a direction", trend.Reason);
        }

        [Fact]
        public void MeasureLocalTrend_PricesRising_MeasuresARateAnnualised()
        {
            // 3000 rising by 30 a month over a year: 360 a year on an average level of about 3165.
            var listings = Series(months: 12, perMonth: 5, startPricePerM2: 3000m, monthlyStep: 30m);

            var trend = GrowthForecastCalculator.MeasureLocalTrend(listings, Now);

            Assert.NotNull(trend.AnnualPercent);
            Assert.InRange(trend.AnnualPercent!.Value, 11m, 12m);
            Assert.False(trend.WasCapped);
            Assert.Equal(12, trend.MonthsObserved);
        }

        [Fact]
        public void MeasureLocalTrend_PricesFalling_MeasuresANegativeRate()
        {
            var listings = Series(months: 12, perMonth: 5, startPricePerM2: 3000m, monthlyStep: -30m);

            var trend = GrowthForecastCalculator.MeasureLocalTrend(listings, Now);

            Assert.NotNull(trend.AnnualPercent);
            Assert.True(trend.AnnualPercent!.Value < 0m);
        }

        // A short violent series can imply any slope at all. Holding it is the difference between
        // a trend and an artefact, and the reason has to say it was held.
        [Fact]
        public void MeasureLocalTrend_AbsurdSlope_IsCappedAndSaysSo()
        {
            var listings = Series(months: 4, perMonth: 10, startPricePerM2: 2000m, monthlyStep: 400m);

            var trend = GrowthForecastCalculator.MeasureLocalTrend(listings, Now);

            Assert.True(trend.WasCapped);
            Assert.Equal(GrowthForecastCalculator.MaximumLocalAnnualPercent, trend.AnnualPercent);
            Assert.Contains("held at", trend.Reason);
        }

        [Fact]
        public void Calculate_NoLocalTrend_RunsEntirelyOnTheSeededRate()
        {
            var noTrend = new GrowthForecastCalculator.LocalTrend(null, false, 0, 0, 0, "no data");

            var forecast = GrowthForecastCalculator.Calculate(100_000m, Seeded(annual: 6m), noTrend, years: 10);

            Assert.Equal(0m, forecast.LocalWeightPercent);
            Assert.Equal(6m, forecast.BlendedAnnualPercent);
            Assert.Null(forecast.LocalAnnualPercent);
        }

        // The local rate is real and current, and it is also three months long. Half is the most
        // it ever gets, so one hot season can never carry a ten year projection on its own.
        [Fact]
        public void Calculate_VeryLongLocalSeries_StillOnlyGetsHalfTheBlend()
        {
            var trend = new GrowthForecastCalculator.LocalTrend(20m, false, 5000, 3650, 120, "long series");

            var forecast = GrowthForecastCalculator.Calculate(100_000m, Seeded(annual: 6m), trend, years: 10);

            Assert.Equal(50m, forecast.LocalWeightPercent);
            Assert.Equal(13m, forecast.BlendedAnnualPercent);
        }

        [Fact]
        public void Calculate_ShortLocalSeries_BarelyMovesTheSeededRate()
        {
            // 90 days of series out of the 730 that would earn a full share.
            var trend = new GrowthForecastCalculator.LocalTrend(20m, false, 500, 90, 3, "short series");

            var forecast = GrowthForecastCalculator.Calculate(100_000m, Seeded(annual: 6m), trend, years: 10);

            Assert.InRange(forecast.LocalWeightPercent, 12m, 13m);
            Assert.InRange(forecast.BlendedAnnualPercent, 7.5m, 8m);
        }

        [Fact]
        public void Calculate_BothRatesSurviveIntoTheAnswer()
        {
            var trend = new GrowthForecastCalculator.LocalTrend(12m, false, 500, 365, 12, "a year of adverts");

            var forecast = GrowthForecastCalculator.Calculate(100_000m, Seeded(annual: 6m), trend, years: 10);

            // The whole point: the blend never swallows the two things it is made of.
            Assert.Equal(6m, forecast.SeededAnnualPercent);
            Assert.Equal(12m, forecast.LocalAnnualPercent);
            Assert.Equal("test assumption", forecast.SeededSource);
            Assert.NotEqual(forecast.SeededAnnualPercent, forecast.BlendedAnnualPercent);
        }

        [Fact]
        public void Calculate_ThreePaths_FanOutByTheDistrictsOwnVolatility()
        {
            var noTrend = new GrowthForecastCalculator.LocalTrend(null, false, 0, 0, 0, "no data");

            var forecast = GrowthForecastCalculator.Calculate(100_000m, Seeded(annual: 6m, volatility: 4m), noTrend, years: 10);

            Assert.Equal(3, forecast.Scenarios.Count);
            Assert.Equal(2m, forecast.Scenarios[0].AnnualPercent);
            Assert.Equal(6m, forecast.Scenarios[1].AnnualPercent);
            Assert.Equal(10m, forecast.Scenarios[2].AnnualPercent);
            Assert.Equal("Base", forecast.Scenarios[1].Name);
        }

        [Fact]
        public void Calculate_PathsCompound_RatherThanAddUp()
        {
            var noTrend = new GrowthForecastCalculator.LocalTrend(null, false, 0, 0, 0, "no data");

            var forecast = GrowthForecastCalculator.Calculate(100_000m, Seeded(annual: 10m, volatility: 0m), noTrend, years: 10);

            var basePath = forecast.Scenarios.Single(x => x.Name == "Base");

            Assert.Equal(11, basePath.Values.Count);
            Assert.Equal(100_000m, basePath.Values[0]);
            Assert.Equal(110_000m, basePath.Values[1]);

            // 10% ten times is 259,374, not 200,000. Simple interest would give the latter.
            Assert.Equal(259_374m, basePath.Values[10]);
        }

        [Fact]
        public void Calculate_ZeroGrowth_LeavesTheValueWhereItIs()
        {
            var noTrend = new GrowthForecastCalculator.LocalTrend(null, false, 0, 0, 0, "no data");

            var forecast = GrowthForecastCalculator.Calculate(250_000m, Seeded(annual: 0m, volatility: 0m), noTrend, years: 5);

            Assert.All(forecast.Scenarios.Single(x => x.Name == "Base").Values, value => Assert.Equal(250_000m, value));
        }
    }
}
