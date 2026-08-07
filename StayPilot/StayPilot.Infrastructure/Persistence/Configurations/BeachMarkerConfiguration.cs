using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StayPilot.Domain.Entities;

namespace StayPilot.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Maps the BeachMarker entity to its table and loads the beach seed data.
    /// The seed rows are written into the table when the migration runs.
    /// </summary>
    public class BeachMarkerConfiguration : IEntityTypeConfiguration<BeachMarker>
    {
        /// <summary>
        /// Sets column rules and adds the fixed list of Algarve beaches.
        /// </summary>
        public void Configure(EntityTypeBuilder<BeachMarker> builder)
        {
            // The database fills the create date on insert (UTC now), not the seed rows below.
            // (Fixed dates in seed rows would make EF think the model keeps changing.)
            builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");

            // Seed data: the known beaches of the Portugal, from OpenStreetMap.
            // The Ids 1-291 stay the same forever, because PropertyListing.NearestBeachMarkerId points to them.
            builder.HasData(AllBeachMarkers.All);
        }
    }
}
