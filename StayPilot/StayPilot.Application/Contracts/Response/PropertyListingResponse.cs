
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Contracts.Response
{
    public class PropertyListingResponse
    {
        public int Id { get; set; }

        public int MarketAreaId { get; set; }

        public string MarketAreaDistrict { get; set; } = string.Empty;

        public string MarketAreaMunicipality { get; set; } = string.Empty;

        public string MarketAreaTown { get; set; } = string.Empty;

        public string MarketAreaZone { get; set; } = string.Empty;

        public PropertyType PropertyType { get; set; }

        public Typology Typology { get; set; }

        public string SourceName { get; set; } = string.Empty;

        public string SourceUrl { get; set; } = string.Empty;

        public int AreaM2 { get; set; }

        public int Bathrooms { get; set; }

        public int? Floor { get; set; }

        public int? TotalFloors { get; set; }

        public bool? HasElevator { get; set; }

        public bool? HasAirConditioning { get; set; }

        public PropertyCondition Condition { get; set; }

        public int? DistanceToBeachMeters { get; set; }

        public int? NearestBeachMarkerId { get; set; }

        public string? NearestBeachName { get; set; }

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
    }
}
