using System.ComponentModel.DataAnnotations;

namespace StayPilot.Application.Contracts.Request
{
    /// <summary>
    /// Request that comes in to list market areas, one page at a time.
    /// The search text is optional: leave it empty to list everything.
    /// </summary>
    public class MarketAreaRequest
    {
        /// <summary>
        /// Free text to look for in district, municipality, town or zone.
        /// It matches on any part of the name, not only the start.
        /// </summary>
        [StringLength(100)]
        public string? Search { get; set; }

        /// <summary>Which page to return. Starts at 1. Allowed values: 1 to 1000.</summary>
        [Range(1, 1000)]
        public int PageNumber { get; set; } = 1;

        /// <summary>How many items per page. Allowed values: 1 to 200.</summary>
        [Range(1, 200)]
        public int PageSize { get; set; } = 20;
    }
}
