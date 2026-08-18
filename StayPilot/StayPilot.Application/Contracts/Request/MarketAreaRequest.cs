using StayPilot.Domain.Enums;
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

        /// <summary>
        /// Which column to sort by. Defaults to the address order the table reads in
        /// (district, then municipality, then town). Sorting happens here and not in the
        /// browser because only one page is ever sent: sorting a page sorts 20 rows out of
        /// thousands, which looks like sorting and is not.
        /// </summary>
        public MarketAreaSortBy SortBy { get; set; } = MarketAreaSortBy.Location;

        /// <summary>True sorts Z to A (or high to low). False (default) sorts the other way.</summary>
        public bool SortDescending { get; set; }
    }
}
