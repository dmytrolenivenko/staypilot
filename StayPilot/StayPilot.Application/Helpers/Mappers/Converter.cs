using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Domain.Entities;

namespace StayPilot.Application.Helpers.Mappers
{
    /// <summary>
    /// Maps between database entities and the request/response shapes.
    /// Each method just copies fields from one object into another.
    /// </summary>
    public class Converter
    {
        /// <summary>
        /// Turn a PropertyListing entity (plus its snapshot) into the response we send back.
        /// The snapshot is optional; pass null if there is none.
        /// </summary>
        public static PropertyListingResponse MapToResponse(PropertyListing property, ListingSnapshot listingSnapshot = null)
        {

            // The market area must be loaded first, because we copy its fields below.
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

        /// <summary>
        /// Turn a PropertyListingRequest (data from the caller) into a PropertyListing entity to save.
        /// </summary>
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

        /// <summary>
        /// Turn a ListingSnapshotRequest (data from the caller) into a ListingSnapshot entity to save.
        /// </summary>
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

        /// <summary>
        /// Turn a ListingSnapshot entity into the snapshot response we send back.
        /// </summary>
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

        /// <summary>
        /// Turn a MarketArea entity into the market area response we send back.
        /// </summary>
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

        /// <summary>
        /// Turn a ListingSnapshot entity into the snapshot response we send back.
        /// (Same result as MapToResponse for a ListingSnapshot.)
        /// </summary>
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
