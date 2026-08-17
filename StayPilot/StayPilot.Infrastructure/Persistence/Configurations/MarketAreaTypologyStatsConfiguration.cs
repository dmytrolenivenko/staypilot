using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StayPilot.Domain.Entities;

namespace StayPilot.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Maps the MarketAreaTypologyStats entity to its table.
    /// No seed data here: every row is worked out from the listings we collected.
    /// </summary>
    public class MarketAreaTypologyStatsConfiguration : IEntityTypeConfiguration<MarketAreaTypologyStats>
    {
        /// <summary>
        /// Sets the column rules and the index that keeps one typology to one row per place.
        /// </summary>
        public void Configure(EntityTypeBuilder<MarketAreaTypologyStats> builder)
        {
            // Money and area: same shapes used on the parent row.
            builder.Property(x => x.MedianPrice).HasPrecision(18, 2);
            builder.Property(x => x.MedianPricePerM2).HasPrecision(18, 2);
            builder.Property(x => x.MedianAreaM2).HasPrecision(10, 2);

            // Store the typology as its name ("T2") and not its number, same reason the level and
            // the premium feature do: reordering the enum cannot then repoint saved rows.
            builder.Property(x => x.Typology).HasConversion<string>();

            // One row per typology per place.
            builder.HasIndex(x => new { x.MarketAreaStatsId, x.Typology }).IsUnique();
        }
    }
}
