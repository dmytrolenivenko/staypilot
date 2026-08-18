using StayPilot.Domain.Entities;

namespace StayPilot.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// The long run house price growth assumed for each Portuguese district.
    ///
    /// READ THIS BEFORE TRUSTING A NUMBER BELOW. These are planning assumptions, not a measured
    /// series. Nobody scraped INE or Confidencial Imobiliario into this table. What they encode
    /// is the shape everyone agrees on - the metropolitan areas and the Algarve have compounded
    /// fastest, the coast has outrun the interior, and the interior districts have barely kept
    /// up with inflation - calibrated so the national row sits on the mid single digit rate the
    /// published national indices have shown over the last decade.
    ///
    /// They are seeded rather than computed because this database holds a few months of adverts,
    /// almost all of them in the Algarve. A ten year projection built on that alone would take
    /// one season of one region and call it Portugal. Every screen that quotes these figures also
    /// prints Source, so the reader can see what they are looking at.
    ///
    /// To correct them: change the numbers here, add a migration, done. Nothing computes off
    /// them at write time, so a correction is a data change and not a recalculation.
    ///
    /// Volatility is how far a single year plausibly lands either side of the growth figure.
    /// Tourist and metropolitan markets swing harder, so their fan is wider.
    /// </summary>
    public static class AllHousePriceGrowth
    {
        private const string Assumption = "StayPilot planning assumption, calibrated to published national and regional trends. Not a measured index.";

        /// <summary>The full seed list. Use with builder.HasData(AllHousePriceGrowth.All).</summary>
        public static readonly HousePriceGrowth[] All = new[]
        {
            // Id 1 is the national fallback: any district with no row of its own lands here.
            new HousePriceGrowth { Id = 1,  District = "",                  AnnualGrowthPercent = 6.0m, VolatilityPercentagePoints = 3.0m, Source = Assumption, AsOfYear = 2026 },

            // Coast, metropolitan and tourist - fastest and most volatile.
            new HousePriceGrowth { Id = 2,  District = "Faro",              AnnualGrowthPercent = 7.5m, VolatilityPercentagePoints = 4.0m, Source = Assumption, AsOfYear = 2026 },
            new HousePriceGrowth { Id = 3,  District = "Lisboa",            AnnualGrowthPercent = 7.0m, VolatilityPercentagePoints = 3.5m, Source = Assumption, AsOfYear = 2026 },
            new HousePriceGrowth { Id = 4,  District = "Porto",             AnnualGrowthPercent = 7.0m, VolatilityPercentagePoints = 3.5m, Source = Assumption, AsOfYear = 2026 },
            new HousePriceGrowth { Id = 5,  District = "Setúbal",           AnnualGrowthPercent = 6.8m, VolatilityPercentagePoints = 3.5m, Source = Assumption, AsOfYear = 2026 },
            new HousePriceGrowth { Id = 6,  District = "Madeira",           AnnualGrowthPercent = 7.0m, VolatilityPercentagePoints = 4.0m, Source = Assumption, AsOfYear = 2026 },

            // Secondary coast and the larger northern centres.
            new HousePriceGrowth { Id = 7,  District = "Braga",             AnnualGrowthPercent = 6.5m, VolatilityPercentagePoints = 3.0m, Source = Assumption, AsOfYear = 2026 },
            new HousePriceGrowth { Id = 8,  District = "Aveiro",            AnnualGrowthPercent = 6.0m, VolatilityPercentagePoints = 3.0m, Source = Assumption, AsOfYear = 2026 },
            new HousePriceGrowth { Id = 9,  District = "Leiria",            AnnualGrowthPercent = 5.5m, VolatilityPercentagePoints = 3.0m, Source = Assumption, AsOfYear = 2026 },
            new HousePriceGrowth { Id = 10, District = "Viana do Castelo",  AnnualGrowthPercent = 5.5m, VolatilityPercentagePoints = 3.0m, Source = Assumption, AsOfYear = 2026 },
            new HousePriceGrowth { Id = 11, District = "Açores",            AnnualGrowthPercent = 5.5m, VolatilityPercentagePoints = 3.5m, Source = Assumption, AsOfYear = 2026 },
            new HousePriceGrowth { Id = 12, District = "Coimbra",           AnnualGrowthPercent = 5.0m, VolatilityPercentagePoints = 2.5m, Source = Assumption, AsOfYear = 2026 },

            // Inland and the south beyond the Algarve - slower, and steadier with it.
            new HousePriceGrowth { Id = 13, District = "Santarém",          AnnualGrowthPercent = 4.5m, VolatilityPercentagePoints = 2.5m, Source = Assumption, AsOfYear = 2026 },
            new HousePriceGrowth { Id = 14, District = "Évora",             AnnualGrowthPercent = 4.5m, VolatilityPercentagePoints = 2.5m, Source = Assumption, AsOfYear = 2026 },
            new HousePriceGrowth { Id = 15, District = "Viseu",             AnnualGrowthPercent = 4.5m, VolatilityPercentagePoints = 2.5m, Source = Assumption, AsOfYear = 2026 },
            new HousePriceGrowth { Id = 16, District = "Vila Real",         AnnualGrowthPercent = 4.0m, VolatilityPercentagePoints = 2.5m, Source = Assumption, AsOfYear = 2026 },
            new HousePriceGrowth { Id = 17, District = "Beja",              AnnualGrowthPercent = 4.0m, VolatilityPercentagePoints = 2.5m, Source = Assumption, AsOfYear = 2026 },
            new HousePriceGrowth { Id = 18, District = "Castelo Branco",    AnnualGrowthPercent = 3.5m, VolatilityPercentagePoints = 2.0m, Source = Assumption, AsOfYear = 2026 },
            new HousePriceGrowth { Id = 19, District = "Bragança",          AnnualGrowthPercent = 3.5m, VolatilityPercentagePoints = 2.0m, Source = Assumption, AsOfYear = 2026 },
            new HousePriceGrowth { Id = 20, District = "Guarda",            AnnualGrowthPercent = 3.0m, VolatilityPercentagePoints = 2.0m, Source = Assumption, AsOfYear = 2026 },
            new HousePriceGrowth { Id = 21, District = "Portalegre",        AnnualGrowthPercent = 3.0m, VolatilityPercentagePoints = 2.0m, Source = Assumption, AsOfYear = 2026 },
        };
    }
}
