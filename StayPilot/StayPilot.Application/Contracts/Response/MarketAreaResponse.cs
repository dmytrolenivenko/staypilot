
namespace StayPilot.Application.Contracts.Response
{
    /// <summary>
    /// Response that carries one market area.
    /// A market area is a place (country, district, municipality, town, zone)
    /// that we group properties under.
    /// </summary>
    public class MarketAreaResponse
    {
        /// <summary>Id of the market area.</summary>
        public int Id { get; set; }

        public string Country { get; set; } = string.Empty;

        public string District { get; set; } = string.Empty;

        public string Municipality { get; set; } = string.Empty;

        public string Town { get; set; } = string.Empty;

        /// <summary>Zone inside the town. Can be empty when the area has no zone.</summary>
        public string? Zone { get; set; }

        /// <summary>Free text notes about the market area.</summary>
        public string? Notes { get; set; }
    }
}
