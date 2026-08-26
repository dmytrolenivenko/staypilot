
using StayPilot.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace StayPilot.Application.Contracts.Request
{
    /// <summary>
    /// Incoming data to add one new property listing.
    /// It carries the address, the property details, the location, and one price snapshot.
    /// The server may find the market area and the nearest beach from this data.
    /// </summary>
    public class PropertyListingRequest
    {
        /// <summary>Market area to use. If empty, the server finds it from the address.</summary>
        public int? MarketAreaId { get; set; }

        /// <summary>Country. Defaults to Portugal.</summary>
        public string? Country { get; set; } = "Portugal";

        /// <summary>District (address part).</summary>
        public string? District { get; set; } = string.Empty;

        /// <summary>Municipality (address part).</summary>
        public string? Municipality { get; set; } = string.Empty;

        /// <summary>Town (address part).</summary>
        public string? Town { get; set; } = string.Empty;

        /// <summary>Zone inside the town. Often not given by the source.</summary>
        public string? Zone { get; set; }

        /// <summary>Kind of property (for example apartment or house).</summary>
        public PropertyType PropertyType { get; set; }

        /// <summary>Number of rooms layout (for example T1, T2).</summary>
        public Typology Typology { get; set; }

        /// <summary>Name of the site the listing came from (for example Idealista).</summary>
        public string SourceName { get; set; } = string.Empty;

        /// <summary>Web address of the listing. Required. Also used to spot duplicates.</summary>
        [Required(AllowEmptyStrings = false)]
        public string SourceUrl { get; set; } = string.Empty;

        /// <summary>Area of the property, in square meters.</summary>
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
        public PropertyCondition Condition { get; set; }

        /// <summary>Year the building was built.</summary>
        public int? ConstructionYear { get; set; }

        /// <summary>Year of the last renovation.</summary>
        public int? RenovationYear { get; set; }

        /// <summary>Number of balconies.</summary>
        public int BalconyCount { get; set; }

        public bool HasTerrace { get; set; }

        public bool HasGarage { get; set; }

        public bool HasParking { get; set; }

        public bool HasSwimmingPool { get; set; }

        public bool IsFurnished { get; set; }

        public bool HasSeaView { get; set; }

        public bool HasCityView { get; set; }

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

        /// <summary>The first price snapshot to save with this property.</summary>
        public ListingSnapshotRequest ListingSnapshot { get; set; } = new ListingSnapshotRequest();
    }
}
