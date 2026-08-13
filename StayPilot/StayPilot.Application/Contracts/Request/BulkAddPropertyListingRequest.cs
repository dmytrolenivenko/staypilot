
using StayPilot.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace StayPilot.Application.Contracts.Request
{
    public class BulkAddPropertyListingRequest
    {
        [MaxLength(10_000)] [Required]
        public List<PropertyListingRequest> Items { get; set; }
    }
}
