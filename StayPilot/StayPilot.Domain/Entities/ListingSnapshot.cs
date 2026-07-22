using StayPilot.Domain.Enums;

namespace StayPilot.Domain.Entities
{
    /// <summary>
    /// A photo in time of a listing: its price and status on a given day.
    /// One property can have many snapshots to show how its price changed.
    /// </summary>
    public class ListingSnapshot
    {
        /// <summary>
        /// Database Id for this snapshot.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Id of the property this snapshot belongs to.
        /// </summary>
        public int PropertyListingId { get; set; }

        /// <summary>
        /// Asking price on this day.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Price for each square meter (Price divided by area).
        /// </summary>
        public decimal PricePerM2 { get; set; }

        /// <summary>
        /// State of the listing on this day (active, sold, price changed).
        /// </summary>
        public ListingStatus Status { get; set; }

        /// <summary>
        /// When this snapshot was taken (UTC time). Defaults to now.
        /// </summary>
        public DateTime SnapshotDateUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The property this snapshot points to.
        /// </summary>
        public PropertyListing PropertyListing { get; set; } = null!;
    }
}
