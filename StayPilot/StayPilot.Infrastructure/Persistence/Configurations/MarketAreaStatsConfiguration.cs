using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StayPilot.Domain.Entities;

namespace StayPilot.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Maps the MarketAreaStats entity to its table.
    /// No seed data here: every row is worked out from the listings we collected.
    /// </summary>
    public class MarketAreaStatsConfiguration : IEntityTypeConfiguration<MarketAreaStats>
    {
        /// <summary>
        /// Sets the column rules and the index that keeps two different places apart.
        /// </summary>
        public void Configure(EntityTypeBuilder<MarketAreaStats> builder)
        {
            // Compare these text columns without caring about case or accents.
            // "CI" = case insensitive, "AI" = accent insensitive. So "Faro" matches "faró".
            // Same collation as MarketArea, because these names are read out of that table.
            builder.Property(x => x.District).UseCollation("Latin1_General_CI_AI");
            builder.Property(x => x.Municipality).UseCollation("Latin1_General_CI_AI");
            builder.Property(x => x.Town).UseCollation("Latin1_General_CI_AI");

            // Money: same shape as the price columns on ListingSnapshot.
            builder.Property(x => x.MedianPricePerM2).HasPrecision(18, 2);
            builder.Property(x => x.ProjectMedianPricePerM2).HasPrecision(18, 2);
            builder.Property(x => x.MoveInMedianPricePerM2).HasPrecision(18, 2);

            // Floor area: same shape as PropertyListing.AreaM2.
            builder.Property(x => x.MedianAreaM2).HasPrecision(10, 2);

            // Coordinates: same accuracy as every other lat/long in the database.
            builder.Property(x => x.CentroidLatitude).HasPrecision(9, 6);
            builder.Property(x => x.CentroidLongitude).HasPrecision(9, 6);

            // Wiping a place's row takes its typology rows with it, so a recalculation cannot
            // leave last run's T2 numbers hanging off nothing.
            builder.HasMany(x => x.TypologyStats)
                .WithOne(x => x.MarketAreaStats)
                .HasForeignKey(x => x.MarketAreaStatsId)
                .OnDelete(DeleteBehavior.Cascade);

            // Store the level as its name ("Municipality") and not its number, so reordering
            // the enum later cannot silently repoint rows that are already saved.
            builder.Property(x => x.Level).HasConversion<string>();

            // One row per place per level. The whole path is needed because town names repeat:
            // Odivelas is a town in Beja and another one in Lisboa, and the two must never merge.
            builder.HasIndex(x => new { x.Level, x.District, x.Municipality, x.Town }).IsUnique();
        }
    }
}
