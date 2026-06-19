
namespace StayPilot.Domain.Entities
{
    public class MarketArea
    {
        public int Id { get; set; }

        public string Country { get; set; } = "Portugal";

        public string District { get; set; } = string.Empty;

        public string Municipality { get; set; } = string.Empty;

        public string Town { get; set; } = string.Empty;

        public string? Zone { get; set; } 

        public string? Notes { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public List<PropertyListing> Properties { get; set; } = new ();
    }
}
