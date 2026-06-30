using StayPilot.Domain.Enums;

namespace StayPilot.Domain.Entities
{
    public class ListingSnapshot
    {
        public int Id { get; set; }

        public int PropertyListingId { get; set; }

        public decimal Price { get; set; }

        public decimal PricePerM2 { get; set; }
        
        public ListingStatus Status { get; set; }

        public DateTime SnapshotDateUtc { get; set; } = DateTime.UtcNow;

        public PropertyListing PropertyListing { get; set; } = null!;
    }
}
