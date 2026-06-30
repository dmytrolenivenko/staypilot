
namespace StayPilot.Application.Contracts.Response
{
    public class MarketAreaResponse
    {
        public int Id { get; set; }

        public string Country { get; set; } = string.Empty;

        public string District { get; set; } = string.Empty;

        public string Municipality { get; set; } = string.Empty;

        public string Town { get; set; } = string.Empty;

        public string? Zone { get; set; }

        public string? Notes { get; set; }
    }
}
