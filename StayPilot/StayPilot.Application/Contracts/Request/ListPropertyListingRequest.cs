
using StayPilot.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace StayPilot.Application.Contracts.Request
{
    /// <summary>
    /// Request that comes in to search property listings, one page at a time.
    /// Every filter is optional (nullable): only the ones you set are used.
    /// Empty filters are ignored, so they match everything.
    /// </summary>
    public class ListPropertyListingRequest
    {
        /// <summary>Keep only properties in this market area.</summary>
        public int? MarketAreaId { get; set; }

        /// <summary>Filter by district (address part).</summary>
        public string? District { get; set; } = string.Empty;

        /// <summary>Filter by municipality (address part).</summary>
        public string? Municipality { get; set; } = string.Empty;

        /// <summary>Filter by town (address part).</summary>
        public string? Town { get; set; } = string.Empty;

        /// <summary>Filter by zone inside the town. Many listings have no zone.</summary>
        public string? Zone { get; set; }

        /// <summary>Kind of property (for example apartment or house).</summary>
        public PropertyType? PropertyType { get; set; }

        /// <summary>Number of rooms layout (for example T1, T2).</summary>
        public Typology? Typology { get; set; }

        /// <summary>Smallest area in square meters.</summary>
        public int? MinAreaM2 { get; set; }

        /// <summary>Largest area in square meters.</summary>
        public int? MaxAreaM2 { get; set; }

        /// <summary>Exact number of bathrooms.</summary>
        public int? Bathrooms { get; set; }

        /// <summary>Which floor the property is on.</summary>
        public int? Floor { get; set; }

        /// <summary>How many floors the building has.</summary>
        public int? TotalFloors { get; set; }

        /// <summary>Only properties with (true) or without (false) an elevator.</summary>
        public bool? HasElevator { get; set; }

        /// <summary>Only properties with (true) or without (false) air conditioning.</summary>
        public bool? HasAirConditioning { get; set; }

        /// <summary>State of the property (for example new, used, needs work).</summary>
        public PropertyCondition? Condition { get; set; }

        /// <summary>Year the building was built.</summary>
        public int? ConstructionYear { get; set; }

        /// <summary>Year of the last renovation.</summary>
        public int? RenovationYear { get; set; }

        /// <summary>Exact number of balconies.</summary>
        public int? BalconyCount { get; set; }

        public bool? HasTerrace { get; set; }

        public bool? HasGarage { get; set; }

        public bool? HasParking { get; set; }

        public bool? HasSwimmingPool { get; set; }

        public bool? IsFurnished { get; set; }

        public bool? HasSeaView { get; set; }

        public bool? HasCityView { get; set; }

        /// <summary>Lowest total price to accept.</summary>
        public decimal? MinPrice { get; set; }

        /// <summary>Highest total price to accept.</summary>
        public decimal? MaxPrice { get; set; }

        /// <summary>Highest price per square meter to accept.</summary>
        public decimal? MaxPricePerM2 { get; set; }

        /// <summary>Lowest price per square meter to accept.</summary>
        public decimal? MinPricePerM2 { get; set; }

        /// <summary>Keep only properties this close (or closer) to the beach, in meters.</summary>
        public int? DistanceToBeachMeters { get; set; }

        /// <summary>Filter by listing state (for example active or sold).</summary>
        public ListingStatus? ListingStatus { get; set; }

        /// <summary>Which page to return. Starts at 1. Allowed values: 1 to 50.</summary>
        [Range(1, 50)]
        public int PageNumber { get; set; } = 1;

        /// <summary>How many items per page. Allowed values: 1 to 20.</summary>
        [Range(1,20)]
        public int PageSize { get; set; } = 20;

        /// <summary>Which field to sort by. Defaults to Id.</summary>
        public SortBy SortBy { get; set; } = SortBy.Id;

        /// <summary>True sorts high to low. False (default) sorts low to high.</summary>
        public bool SortDescending { get; set; }
    }
}
