
using StayPilot.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace StayPilot.Application.Contracts.Request
{
    public class PropertyListingRequest
    {
        public int? MarketAreaId { get; set; }

        public string? Country { get; set; } = "Portugal";

        public string? District { get; set; } = string.Empty;

        public string? Municipality { get; set; } = string.Empty;

        public string? Town { get; set; } = string.Empty;

        public string? Zone { get; set; }

        public PropertyType PropertyType { get; set; }

        public Typology Typology { get; set; }

        public string SourceName { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false)]
        public string SourceUrl { get; set; } = string.Empty;

        public int AreaM2 { get; set; }

        public int Bathrooms { get; set; }

        public int? Floor { get; set; }

        public int? TotalFloors { get; set; }

        public bool? HasElevator { get; set; }

        public bool? HasAirConditioning { get; set; }

        public PropertyCondition Condition { get; set; }

        public int? ConstructionYear { get; set; }

        public int? RenovationYear { get; set; }

        public int BalconyCount { get; set; }

        public bool HasTerrace { get; set; }

        public bool HasGarage { get; set; }

        public bool HasParking { get; set; }

        public bool HasSwimmingPool { get; set; }

        public bool IsFurnished { get; set; }

        public bool HasSeaView { get; set; }

        public bool HasCityView { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        public string? EnergyCertificate { get; set; }

        public string? Notes { get; set; }

        public ListingSnapshotRequest ListingSnapshot { get; set; } = new ListingSnapshotRequest();
    }
}
