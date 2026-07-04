using StayPilot.Domain.Enums;

namespace StayPilot.Application.Contracts.Request
{
    public class ListingSnapshotRequest
    {
        public int PropertyListingId { get; set; }

        public decimal Price { get; set; }

        public decimal PricePerM2 { get; set; }

        public ListingStatus Status { get; set; }

        public DateTime SnapshotDateUtc { get; set; } = DateTime.UtcNow;
    }
}
