
namespace StayPilot.Domain.Entities
{
    /// <summary>
    /// One beach point on the map.
    /// We use it to find the nearest beach to a property.
    /// </summary>
    public class BeachMarker
    {
        /// <summary>
        /// Our own database Id for this beach.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Id of this beach in OpenStreetMap (the source of the data).
        /// </summary>
        public long OsmId { get; set; }

        /// <summary>
        /// Beach name. Can be empty if the source has no name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Beach location: north-south position.
        /// </summary>
        public decimal Latitude { get; set; }

        /// <summary>
        /// Beach location: east-west position.
        /// </summary>
        public decimal Longitude { get; set; }

        /// <summary>
        /// Area or region name where the beach is. Can be empty.
        /// </summary>
        public string? Region { get; set; }

        /// <summary>
        /// When we saved this beach (UTC time).
        /// </summary>
        public DateTime CreatedAtUtc { get; set; }
    }
}
