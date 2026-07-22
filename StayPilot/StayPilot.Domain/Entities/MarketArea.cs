
namespace StayPilot.Domain.Entities
{
    /// <summary>
    /// One place on the map (country, district, town, zone).
    /// We group properties by market area to compare them.
    /// </summary>
    public class MarketArea
    {
        /// <summary>
        /// Database Id for this market area.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Country name. Defaults to Portugal.
        /// </summary>
        public string Country { get; set; } = "Portugal";

        /// <summary>
        /// District (the largest area inside the country).
        /// </summary>
        public string District { get; set; } = string.Empty;

        /// <summary>
        /// Municipality (a smaller area inside the district).
        /// </summary>
        public string Municipality { get; set; } = string.Empty;

        /// <summary>
        /// Town name.
        /// </summary>
        public string Town { get; set; } = string.Empty;

        /// <summary>
        /// Zone (a small part of a town). Can be empty; many sources do not give it.
        /// </summary>
        public string? Zone { get; set; }

        /// <summary>
        /// Free text notes for humans. Can be empty.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// When we saved this market area (UTC time).
        /// </summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>
        /// All properties that belong to this market area.
        /// </summary>
        public List<PropertyListing> Properties { get; set; } = new ();
    }
}
