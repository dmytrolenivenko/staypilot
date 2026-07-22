using StayPilot.Domain.Enums;

namespace StayPilot.Application.Contracts.Request
{
    /// <summary>
    /// Incoming data for one snapshot of a listing.
    /// A snapshot is the price and state of a property at one moment in time.
    /// It is sent as part of adding a property (see PropertyListingRequest).
    /// </summary>
    public class ListingSnapshotRequest
    {
        /// <summary>Id of the property this snapshot belongs to.</summary>
        public int PropertyListingId { get; set; }

        /// <summary>Total price at this moment.</summary>
        public decimal Price { get; set; }

        /// <summary>Price divided by area, in price per square meter.</summary>
        public decimal PricePerM2 { get; set; }

        /// <summary>State of the listing at this moment (for example active or sold).</summary>
        public ListingStatus Status { get; set; }

        /// <summary>When this snapshot was taken. In UTC. Defaults to now.</summary>
        public DateTime SnapshotDateUtc { get; set; } = DateTime.UtcNow;
    }
}
