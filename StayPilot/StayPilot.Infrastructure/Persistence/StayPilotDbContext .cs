using Microsoft.EntityFrameworkCore;
using StayPilot.Domain.Entities;

namespace StayPilot.Infrastructure.Persistence
{
    public class StayPilotDbContext : DbContext
    {
        public StayPilotDbContext(DbContextOptions<StayPilotDbContext> options) : base(options) { }

        public DbSet<PropertyListing> PropertyListings => Set<PropertyListing>();

        public DbSet<MarketArea> MarketAreas => Set<MarketArea>();

        public DbSet<ListingSnapshot> ListingSnapshots => Set<ListingSnapshot>();

        public DbSet<OwnedProperty> OwnedProperties => Set<OwnedProperty>();

        public DbSet<BeachMarker> BeachMarkers => Set<BeachMarker>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Wire up the MarketArea
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(StayPilotDbContext).Assembly);

            // Special parameters
            modelBuilder.Entity<PropertyListing>().HasIndex(x => x.SourceUrl).IsUnique();

            modelBuilder.Entity<PropertyListing>().Property(x => x.Latitude).HasPrecision(9, 6);

            modelBuilder.Entity<PropertyListing>().Property(x => x.Longitude).HasPrecision(9, 6);

            modelBuilder.Entity<PropertyListing>().Property(x => x.AreaM2).HasPrecision(10, 2);

            modelBuilder.Entity<ListingSnapshot>().Property(x => x.Price).HasPrecision(18, 2);

            modelBuilder.Entity<ListingSnapshot>().Property(x => x.PricePerM2).HasPrecision(18, 2);

            modelBuilder.Entity<ListingSnapshot>().HasIndex(x => new { x.PropertyListingId, x.SnapshotDateUtc });

            modelBuilder.Entity<OwnedProperty>().Property(x => x.PurchasePrice).HasPrecision(18, 2);

            modelBuilder.Entity<OwnedProperty>().Property(x => x.RenovationInvestment).HasPrecision(18, 2);

            modelBuilder.Entity<OwnedProperty>().Property(x => x.Latitude).HasPrecision(9, 6);

            modelBuilder.Entity<OwnedProperty>().Property(x => x.Longitude).HasPrecision(9, 6);

            modelBuilder.Entity<BeachMarker>().Property(x => x.Latitude).HasPrecision(9, 6);

            modelBuilder.Entity<BeachMarker>().Property(x => x.Longitude).HasPrecision(9, 6);
        }
    }
}
