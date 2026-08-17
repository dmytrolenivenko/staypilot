
using System.ComponentModel.DataAnnotations;
using StayPilot.Application.Contracts.Response.Base;

namespace StayPilot.Application.Contracts.Response
{
    /// <summary>
    /// Response sent back for a paged search of properties.
    /// It carries one page of properties plus the paging info,
    /// so the caller can work out how many pages exist.
    /// </summary>
    public class FilterPropertyListingResponse : ResponseBase
    {
        /// <summary>The properties found for this page.</summary>
        public List<PropertyListingResponse> Items { get; set; } = new();

        /// <summary>Which page this is. Starts at 1.</summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>How many items per page.</summary>
        [Range(1, 20)]
        public int PageSize { get; set; } = 20;

        /// <summary>Total number of matches across all pages (not just this page).</summary>
        public int TotalRecords { get; set; }

    }
}
