using StayPilot.Application.Contracts.Response.Base;
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Contracts.Response
{
    /// <summary>
    /// Response data for one snapshot of a listing.
    /// A snapshot is the price and state of a property at one moment in time.
    /// It is sent back inside a property listing response.
    /// </summary>
    public class ListingSnapshotResponse : ResponseBase
    {
        /// <summary>Id of this snapshot.</summary>
        public int Id { get; set; }

        /// <summary>Id of the property this snapshot belongs to.</summary>
        public int PropertyListingId { get; set; }

        /// <summary>Total price at this moment.</summary>
        public decimal Price { get; set; }

        /// <summary>Price divided by area, in price per square meter.</summary>
        public decimal PricePerM2 { get; set; }

        /// <summary>State of the listing at this moment (for example active or sold).</summary>
        public ListingStatus Status { get; set; }

        /// <summary>When this snapshot was taken. In UTC.</summary>
        public DateTime SnapshotDateUtc { get; set; } = DateTime.UtcNow;
    }
}
