
using StayPilot.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace StayPilot.Application.Contracts.Request
{
    public class ListPropertyListingRequest
    {
        public int? MarketAreaId { get; set; }

        public string? Location { get; set; }

        public PropertyType? PropertyType { get; set; }

        public Typology? Typology { get; set; }

        public int? MinAreaM2 { get; set; }

        public int? MaxAreaM2 { get; set; }

        public int? Bathrooms { get; set; }

        public int? Floor { get; set; }

        public int? TotalFloors { get; set; }

        public bool? HasElevator { get; set; }

        public bool? HasAirConditioning { get; set; }

        public PropertyCondition? Condition { get; set; }

        public int? ConstructionYear { get; set; }

        public int? RenovationYear { get; set; }

        public int? BalconyCount { get; set; }

        public bool? HasTerrace { get; set; }

        public bool? HasGarage { get; set; }

        public bool? HasParking { get; set; }

        public bool? HasSwimmingPool { get; set; }

        public bool? IsFurnished { get; set; }

        public bool? HasSeaView { get; set; }

        public bool? HasCityView { get; set; }

        public decimal? MinPrice { get; set; }

        public decimal? MaxPrice { get; set; }

        public decimal? MaxPricePerM2 { get; set; }

        public decimal? MinPricePerM2 { get; set; }

        public int? DistanceToBeachMeters { get; set; }

        public ListingStatus? ListingStatus { get; set; }

        [Range(1, 50)]
        public int PageNumber { get; set; } = 1;

        [Range(1,20)]
        public int PageSize { get; set; } = 20;

        public SortBy SortBy { get; set; } = SortBy.Id;

        public bool SortDescending { get; set; }
    }
}
