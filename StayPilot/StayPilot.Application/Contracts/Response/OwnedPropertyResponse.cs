
using StayPilot.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace StayPilot.Application.Contracts.Response
{
    public class OwnedPropertyResponse
    {
        // Fix: Id was missing, so a caller (and Converter.MapToResponse, which
        // already tries to set it) had no way to say which row this is about.
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public int MarketAreaId { get; set; }

        /// <summary>
        /// Price the user paid to buy this property.
        /// </summary>
        public decimal? PurchasePrice { get; set; }

        /// <summary>
        /// Date the user bought this property.
        /// </summary>
        public DateTime? PurchaseDate { get; set; }

        /// <summary>Kind of property (for example apartment or house).</summary>
        [Required]
        public PropertyType PropertyType { get; set; }

        /// <summary>Number of rooms layout (for example T1, T2).</summary>
        [Required]
        public Typology Typology { get; set; }

        /// <summary>Area of the property, in square meters.</summary>
        [Range(1, 1000000)]
        public int AreaM2 { get; set; }

        /// <summary>Number of bathrooms.</summary>
        public int Bathrooms { get; set; }

        /// <summary>Which floor the property is on.</summary>
        public int? Floor { get; set; }

        /// <summary>How many floors the building has.</summary>
        public int? TotalFloors { get; set; }

        public bool? HasElevator { get; set; }

        public bool? HasAirConditioning { get; set; }

        /// <summary>State of the property (for example new, used, needs work).</summary>
        public PropertyCondition? Condition { get; set; }

        /// <summary>Year the building was built.</summary>
        public int? ConstructionYear { get; set; }

        /// <summary>Year of the last renovation.</summary>
        public int? RenovationYear { get; set; }

        /// <summary>/// Money spent on repairs or upgrades. Can be empty./// </summary>
        public decimal? RenovationInvestment { get; set; }

        /// <summary>Number of balconies.</summary>
        public int? BalconyCount { get; set; }

        public bool? HasTerrace { get; set; }

        public bool? HasGarage { get; set; }

        public bool? HasParking { get; set; }

        public bool? HasSwimmingPool { get; set; }

        public bool? IsFurnished { get; set; }

        public bool? HasSeaView { get; set; }

        public bool? HasCityView { get; set; }

        /// <summary>How far the property is from the nearest beach, in meters.</summary>
        public int? DistanceToBeachMeters { get; set; }

        /// <summary>Id of the nearest beach marker.</summary>
        public int? NearestBeachMarkerId { get; set; }

        /// <summary>Name of the nearest beach.</summary>
        public string? NearestBeachName { get; set; }

        /// <summary>Energy rating of the property (for example A, B, C).</summary>
        public string? EnergyCertificate { get; set; }

        /// <summary>Free text notes about the property.</summary>
        public string? Notes { get; set; }
    }
}
