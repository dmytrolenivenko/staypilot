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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PropertyListing>().HasIndex(x => x.SourceUrl).IsUnique();

            modelBuilder.Entity<ListingSnapshot>().Property(x => x.Price).HasPrecision(18, 2);

            modelBuilder.Entity<OwnedProperty>().Property(x => x.PurchasePrice).HasPrecision(18, 2);

            modelBuilder.Entity<OwnedProperty>().Property(x => x.RenovationInvestment).HasPrecision(18, 2);
        }
    }
}
