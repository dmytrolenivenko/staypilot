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

            // Store the level as its name ("Municipality") and not its number, so reordering
            // the enum later cannot silently repoint rows that are already saved.
            builder.Property(x => x.Level).HasConversion<string>();

            // One row per place per level. The whole path is needed because town names repeat:
            // Odivelas is a town in Beja and another one in Lisboa, and the two must never merge.
            builder.HasIndex(x => new { x.Level, x.District, x.Municipality, x.Town }).IsUnique();
        }
    }
}
