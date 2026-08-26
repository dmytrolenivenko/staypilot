
using StayPilot.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace StayPilot.Application.Contracts.Request
{
    public class OwnedPropertyRequest
    {
        /// <summary>/// A short name the user gives to this property./// </summary>
        [Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>Country. Defaults to Portugal.</summary>
        public string? Country { get; set; } = "Portugal";

        /// <summary>District (address part).</summary>
        public string District { get; set; } = string.Empty;

        /// <summary>Municipality (address part).</summary>
        public string Municipality { get; set; } = string.Empty;

        /// <summary>Town (address part).</summary>
        public string Town { get; set; } = string.Empty;

        /// <summary>Zone inside the town. Often not given by the source.</summary>
        public string? Zone { get; set; }

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

        /// <summary>Latitude of the property. Needed to find the nearest beach.</summary>
        [Range(-90, 90)]
        public decimal Latitude { get; set; }

        /// <summary>Longitude of the property. Needed to find the nearest beach.</summary>
        [Range(-180, 180)]
        public decimal Longitude { get; set; }

        /// <summary>Energy rating of the property (for example A, B, C).</summary>
        public string? EnergyCertificate { get; set; }

        /// <summary>Free text notes about the property.</summary>
        public string? Notes { get; set; }
    }
}
