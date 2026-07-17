using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Domain.Entities;

namespace StayPilot.Application.Helpers.Mappers
{
    public class Converter
    {
        public static PropertyListingResponse MapToResponse(PropertyListing property, ListingSnapshot listingSnapshot = null)
        {

            if (property.MarketArea == null)
            {
                throw new InvalidOperationException("MarketArea must be loaded before mapping.");
            }

            return new PropertyListingResponse
            {
                Id = property.Id,
                MarketAreaId = property.MarketAreaId,
                MarketAreaDistrict = property.MarketArea.District,
                MarketAreaMunicipality = property.MarketArea.Municipality,
                MarketAreaTown = property.MarketArea.Town,
                MarketAreaZone = property.MarketArea.Zone ?? string.Empty,
                PropertyType = property.PropertyType,
                Typology = property.Typology,
                SourceName = property.SourceName,
                SourceUrl = property.SourceUrl,
                AreaM2 = property.AreaM2,
                Bathrooms = property.Bathrooms,
                Floor = property.Floor,
                TotalFloors = property.TotalFloors,
                HasElevator = property.HasElevator,
                HasAirConditioning = property.HasAirConditioning,
                Condition = property.Condition,
                ConstructionYear = property.ConstructionYear,
                DistanceToBeachMeters = property.DistanceToBeachMeters,
                NearestBeachMarkerId = property.NearestBeachMarkerId,
                NearestBeachName = property.NearestBeachName,
                RenovationYear = property.RenovationYear,
                BalconyCount = property.BalconyCount,
                HasTerrace = property.HasTerrace,
                HasGarage = property.HasGarage,
                HasParking = property.HasParking,
                HasSwimmingPool = property.HasSwimmingPool,
                IsFurnished = property.IsFurnished,
                HasSeaView = property.HasSeaView,
                HasCityView = property.HasCityView,
                Latitude = property.Latitude,
                Longitude = property.Longitude,
                EnergyCertificate = property.EnergyCertificate,
                Notes = property.Notes,
                ListingSnapshot = listingSnapshot is not null ? MapToResponse(listingSnapshot) : null
            };
        }

        public static PropertyListing MapToEntity(PropertyListingRequest property)
        {
            return new PropertyListing
            {
                PropertyType = property.PropertyType,
                Typology = property.Typology,
                SourceName = property.SourceName,
                SourceUrl = property.SourceUrl,
                AreaM2 = property.AreaM2,
                Bathrooms = property.Bathrooms,
                Floor = property.Floor,
                TotalFloors = property.TotalFloors,
                HasElevator = property.HasElevator,
                HasAirConditioning = property.HasAirConditioning,
                Condition = property.Condition,
                ConstructionYear = property.ConstructionYear,
                RenovationYear = property.RenovationYear,
                BalconyCount = property.BalconyCount,
                HasTerrace = property.HasTerrace,
                HasGarage = property.HasGarage,
                HasParking = property.HasParking,
                HasSwimmingPool = property.HasSwimmingPool,
                IsFurnished = property.IsFurnished,
                HasSeaView = property.HasSeaView,
                HasCityView = property.HasCityView,
                Latitude = property.Latitude,
                Longitude = property.Longitude,
                EnergyCertificate = property.EnergyCertificate,
                Notes = property.Notes,
            };
        }

        public static ListingSnapshot MapToEntity(ListingSnapshotRequest snapshot)
        {
            return new ListingSnapshot
            {
                Price = snapshot.Price,
                PricePerM2 = snapshot.PricePerM2,
                Status = snapshot.Status,
                SnapshotDateUtc = snapshot.SnapshotDateUtc
            };
        }

        public static ListingSnapshotResponse MapToResponse(ListingSnapshot snapshot)
        {
            return new ListingSnapshotResponse
            {
                Id = snapshot.Id,
                PropertyListingId = snapshot.PropertyListingId,
                Price = snapshot.Price,
                PricePerM2 = snapshot.PricePerM2,
                Status = snapshot.Status,
                SnapshotDateUtc = snapshot.SnapshotDateUtc
            };
        }

        public static MarketAreaResponse MapToResponse(MarketArea marketArea)
        {
            return new MarketAreaResponse
            {
                Id = marketArea.Id,
                Country = marketArea.Country,
                District = marketArea.District,
                Municipality = marketArea.Municipality,
                Town = marketArea.Town,
                Zone = marketArea.Zone,
                Notes = marketArea.Notes
            };
        }

        public static ListingSnapshotResponse MapEntityToResponse(ListingSnapshot snapshot)
        {
            return new ListingSnapshotResponse
            {
                Id = snapshot.Id,
                PropertyListingId = snapshot.PropertyListingId,
                Price = snapshot.Price,
                PricePerM2 = snapshot.PricePerM2,
                Status = snapshot.Status,
                SnapshotDateUtc = snapshot.SnapshotDateUtc
            };
        }


    }
}
