using StayPilot.Domain.Enums;

namespace StayPilot.Application.Contracts.Response
{
    public class ListingSnapshotResponse
    {
        public int Id { get; set; }
        public int PropertyListingId { get; set; }

        public decimal Price { get; set; }

        public decimal PricePerM2 { get; set; }

        public ListingStatus Status { get; set; }

        public DateTime SnapshotDateUtc { get; set; } = DateTime.UtcNow;
    }
}
