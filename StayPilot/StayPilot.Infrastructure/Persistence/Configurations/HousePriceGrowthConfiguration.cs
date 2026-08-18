using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StayPilot.Domain.Entities;

namespace StayPilot.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Maps HousePriceGrowth to its table and loads the seeded district assumptions.
    ///
    /// The district is unique, so the lookup can be a single-row read and a second row for the
    /// same district cannot quietly win. The empty-string district is the national fallback and
    /// occupies that unique slot like any other.
    /// </summary>
    public class HousePriceGrowthConfiguration : IEntityTypeConfiguration<HousePriceGrowth>
    {
        /// <summary>
        /// Sets column rules and adds the fixed list of per-district growth assumptions.
        /// </summary>
        public void Configure(EntityTypeBuilder<HousePriceGrowth> builder)
        {
            builder.Property(x => x.District).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Source).HasMaxLength(400).IsRequired();

            // One decimal place is all the precision an assumption deserves. Storing more would
            // dress a planning figure up as a measurement.
            builder.Property(x => x.AnnualGrowthPercent).HasPrecision(5, 2);
            builder.Property(x => x.VolatilityPercentagePoints).HasPrecision(5, 2);

            builder.HasIndex(x => x.District).IsUnique();

            builder.HasData(AllHousePriceGrowth.All);
        }
    }
}
