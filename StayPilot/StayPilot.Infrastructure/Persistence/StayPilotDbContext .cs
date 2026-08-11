using Microsoft.EntityFrameworkCore;
using StayPilot.Domain.Entities;

namespace StayPilot.Infrastructure.Persistence
{
    /// <summary>
    /// The database context: our door to the database.
    /// Each DbSet below is one table. EF uses this class to read and save data.
    /// </summary>
    public class StayPilotDbContext : DbContext
    {
        public StayPilotDbContext(DbContextOptions<StayPilotDbContext> options) : base(options) { }

        /// <summary>The properties (homes/flats) table.</summary>
        public DbSet<PropertyListing> PropertyListings => Set<PropertyListing>();

        /// <summary>The market areas table (country, district, town, zone).</summary>
        public DbSet<MarketArea> MarketAreas => Set<MarketArea>();

        /// <summary>The snapshots table: price and state of a property at one point in time.</summary>
        public DbSet<ListingSnapshot> ListingSnapshots => Set<ListingSnapshot>();

        /// <summary>The properties we own ourselves.</summary>
        public DbSet<OwnedProperty> OwnedProperties => Set<OwnedProperty>();

        /// <summary>The beaches table (used to find the nearest beach to a property).</summary>
        public DbSet<BeachMarker> BeachMarkers => Set<BeachMarker>();

        ///<summary>The premium feature table holds the features that can influence the base price</summary>
        public DbSet<PremiumFeature> PremiumFeatures => Set<PremiumFeature>();

        /// <summary>
        /// Builds the database shape: tables, keys, indexes, and number precision.
        /// EF calls this once when it first needs the model.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Load all the per-entity setup classes (like BeachMarkerConfiguration and MarketAreaConfiguration).
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(StayPilotDbContext).Assembly);

            // Special parameters
            // Two properties can not share the same source URL (stops duplicates).
            modelBuilder.Entity<PropertyListing>().HasIndex(x => x.SourceUrl).IsUnique();

            // Store map coordinates with 6 digits after the point (about 0.1 m of accuracy).
            modelBuilder.Entity<PropertyListing>().Property(x => x.Latitude).HasPrecision(9, 6);

            modelBuilder.Entity<PropertyListing>().Property(x => x.Longitude).HasPrecision(9, 6);

            // Floor area: up to 10 digits, 2 after the point.
            modelBuilder.Entity<PropertyListing>().Property(x => x.AreaM2).HasPrecision(10, 2);

            // The database fills the create date on insert (UTC now).
            modelBuilder.Entity<PropertyListing>().Property(x => x.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");



            // Money values: up to 18 digits, 2 after the point.
            modelBuilder.Entity<ListingSnapshot>().Property(x => x.Price).HasPrecision(18, 2);

            modelBuilder.Entity<ListingSnapshot>().Property(x => x.PricePerM2).HasPrecision(18, 2);

            // Index to quickly find a property's snapshots and sort them by date.
            modelBuilder.Entity<ListingSnapshot>().HasIndex(x => new { x.PropertyListingId, x.SnapshotDateUtc });



            // Money values for owned properties: up to 18 digits, 2 after the point.
            modelBuilder.Entity<OwnedProperty>().Property(x => x.PurchasePrice).HasPrecision(18, 2);

            modelBuilder.Entity<OwnedProperty>().Property(x => x.RenovationInvestment).HasPrecision(18, 2);

            modelBuilder.Entity<OwnedProperty>().Property(x => x.Latitude).HasPrecision(9, 6);

            modelBuilder.Entity<OwnedProperty>().Property(x => x.Longitude).HasPrecision(9, 6);



            // Beach coordinates: same accuracy as the properties.
            modelBuilder.Entity<BeachMarker>().Property(x => x.Latitude).HasPrecision(9, 6);

            modelBuilder.Entity<BeachMarker>().Property(x => x.Longitude).HasPrecision(9, 6);

            // Premuin features data precision digits
            modelBuilder.Entity<PremiumFeature>().Property(x => x.PremiumPercent).HasPrecision(9, 2);
            modelBuilder.Entity<PremiumFeature>().Property(x => x.LowerBoundPercent).HasPrecision(9, 2);
            modelBuilder.Entity<PremiumFeature>().Property(x => x.UpperBoundPercent).HasPrecision(9, 2);
            modelBuilder.Entity<PremiumFeature>().Property(x => x.MaximumPercent).HasPrecision(9, 2);

            // Store the Feature enum as its NAME ("HasGarage"), not its int. Keeps the column
            // nvarchar so existing rows stay valid (names match the enum members exactly), the
            // table stays human-readable, and reordering the enum can't silently repoint rows.
            modelBuilder.Entity<PremiumFeature>().Property(x => x.Feature).HasConversion<string>();
        }
    }
}
