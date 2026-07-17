
using System.ComponentModel.DataAnnotations;

namespace StayPilot.Application.Contracts.Response
{
    public class ListPropertyListingResponse
    {
        public List<PropertyListingResponse> Items { get; set; } = new();

        public int PageNumber { get; set; } = 1;

        [Range(1, 20)]
        public int PageSize { get; set; } = 20;

        public int TotalRecords { get; set; }

    }
}
