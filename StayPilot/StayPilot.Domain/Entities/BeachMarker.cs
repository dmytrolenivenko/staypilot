
namespace StayPilot.Domain.Entities
{
    public class BeachMarker
    {
        public int Id { get; set; }

        public long OsmId { get; set; }

        public string? Name { get; set; }

        public decimal Latitude { get; set; }
        
        public decimal Longitude { get; set; }

        public string? Region { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
