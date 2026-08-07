using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StayPilot.Domain.Entities;


namespace StayPilot.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Maps the MarketArea entity to its table and loads the market area seed data.
    /// The seed rows are written into the table when the migration runs.
    /// </summary>
    public class MarketAreaConfiguration : IEntityTypeConfiguration<MarketArea>
    {
        /// <summary>
        /// Sets column rules and adds the fixed list of Portugal market areas.
        /// </summary>
        public void Configure(EntityTypeBuilder<MarketArea> builder)
        {
            // The database fills the create date on insert (UTC now).
            builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");

            // Compare these text columns without caring about case or accents.
            // "CI" = case insensitive, "AI" = accent insensitive. So "Faro" matches "faró".
            builder.Property(x => x.District).UseCollation("Latin1_General_CI_AI");
            builder.Property(x => x.Municipality).UseCollation("Latin1_General_CI_AI");
            builder.Property(x => x.Town).UseCollation("Latin1_General_CI_AI");
            builder.Property(x => x.Zone).UseCollation("Latin1_General_CI_AI");

            // Seed data: all the Portugal market areas (district / municipality / town / zone) taken from Idealista.
            builder.HasData(AllMarketAreas.All);
    }
    }
}
