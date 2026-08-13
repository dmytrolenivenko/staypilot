
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Contracts.Response
{
    /// <summary>
    /// Response that carries one full property listing sent back to the caller.
    /// It holds the property details, its market area, its location,
    /// the nearest beach, and the latest price snapshot.
    /// </summary>
    public class PropertyListingResponse
    {
        /// <summary>Id of the property.</summary>
        public int Id { get; set; }

        /// <summary>Id of the market area this property belongs to.</summary>
        public int MarketAreaId { get; set; }

        /// <summary>District of the market area (copied here for easy reading).</summary>
        public string MarketAreaDistrict { get; set; } = string.Empty;

        /// <summary>Municipality of the market area.</summary>
        public string MarketAreaMunicipality { get; set; } = string.Empty;

        /// <summary>Town of the market area.</summary>
        public string MarketAreaTown { get; set; } = string.Empty;

        /// <summary>Zone of the market area. Can be empty.</summary>
        public string MarketAreaZone { get; set; } = string.Empty;

        /// <summary>Kind of property (for example apartment or house).</summary>
        public PropertyType PropertyType { get; set; }

        /// <summary>Number of rooms layout (for example T1, T2).</summary>
        public Typology Typology { get; set; }

        /// <summary>Name of the site the listing came from.</summary>
        public string SourceName { get; set; } = string.Empty;

        /// <summary>Web address of the listing.</summary>
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

        /// <summary>How far the property is from the nearest beach, in meters.</summary>
        public int? DistanceToBeachMeters { get; set; }

        /// <summary>Id of the nearest beach marker.</summary>
        public int? NearestBeachMarkerId { get; set; }

        /// <summary>Name of the nearest beach.</summary>
        public string? NearestBeachName { get; set; }

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

        /// <summary>Latitude of the property.</summary>
        public decimal? Latitude { get; set; }

        /// <summary>Longitude of the property.</summary>
        public decimal? Longitude { get; set; }

        /// <summary>Energy rating of the property (for example A, B, C).</summary>
        public string? EnergyCertificate { get; set; }

        /// <summary>Free text notes about the property.</summary>
        public string? Notes { get; set; }

        /// <summary>Latest price snapshot. Can be null if there is none.</summary>
        public ListingSnapshotResponse? ListingSnapshot { get; set; }

        /// <summary>Flag if the property listing was added successfully.</summary>
        public bool IsNew { get; set; } = false;

        /// <summary>Flag if the property snapshot was updated successfully.</summary>
        public bool IsSnapshotUpdated { get; set; } = false;
    }
}
