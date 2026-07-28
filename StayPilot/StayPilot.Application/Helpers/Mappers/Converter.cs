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
        public static PropertyListingResponse MapToResponse(PropertyListing property, ListingSnapshot? listingSnapshot = null)
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
                PropertyListingId = snapshot.PropertyListingId,
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
        /// Puts the request's values onto an existing property, for Update only.
        /// Name/PropertyType/Typology/AreaM2/Bathrooms are always sent (they are
        /// required), so we always set them. Everything else is optional: we only
        /// change it when the caller actually sent a value, so a request that
        /// leaves a field out does not blank the value already saved.
        /// </summary>
        public static void ApplyUpdates(OwnedProperty entity, OwnedPropertyRequest request)
        {
            entity.Name = request.Name;
            entity.PropertyType = request.PropertyType;
            entity.Typology = request.Typology;
            entity.AreaM2 = request.AreaM2;
            entity.Bathrooms = request.Bathrooms;

            if (request.Floor is not null) entity.Floor = request.Floor;
            if (request.TotalFloors is not null) entity.TotalFloors = request.TotalFloors;
            if (request.HasElevator is not null) entity.HasElevator = request.HasElevator;
            if (request.HasAirConditioning is not null) entity.HasAirConditioning = request.HasAirConditioning;
            if (request.Condition is not null) entity.Condition = request.Condition.Value;
            if (request.ConstructionYear is not null) entity.ConstructionYear = request.ConstructionYear;
            if (request.RenovationYear is not null) entity.RenovationYear = request.RenovationYear;
            if (request.RenovationInvestment is not null) entity.RenovationInvestment = request.RenovationInvestment;
            if (request.BalconyCount is not null) entity.BalconyCount = request.BalconyCount.Value;
            if (request.HasTerrace is not null) entity.HasTerrace = request.HasTerrace.Value;
            if (request.HasGarage is not null) entity.HasGarage = request.HasGarage.Value;
            if (request.HasParking is not null) entity.HasParking = request.HasParking.Value;
            if (request.HasSwimmingPool is not null) entity.HasSwimmingPool = request.HasSwimmingPool.Value;
            if (request.IsFurnished is not null) entity.IsFurnished = request.IsFurnished.Value;
            if (request.HasSeaView is not null) entity.HasSeaView = request.HasSeaView.Value;
            if (request.HasCityView is not null) entity.HasCityView = request.HasCityView.Value;
            if (request.EnergyCertificate is not null) entity.EnergyCertificate = request.EnergyCertificate;
            if (request.Notes is not null) entity.Notes = request.Notes;
            if (request.PurchasePrice is not null) entity.PurchasePrice = request.PurchasePrice.Value;
            if (request.PurchaseDate is not null) entity.PurchaseDate = request.PurchaseDate.Value;
        }

        public static OwnedProperty MapToEntity(OwnedPropertyRequest request)
        {
            return new OwnedProperty
            {
                Name = request.Name,
                PropertyType = request.PropertyType,
                Typology = request.Typology,
                AreaM2 = request.AreaM2,
                Bathrooms = request.Bathrooms,
                Floor = request.Floor,
                TotalFloors = request.TotalFloors,
                HasElevator = request.HasElevator,
                Condition = request.Condition ?? default,
                ConstructionYear = request.ConstructionYear,
                RenovationYear = request.RenovationYear,
                RenovationInvestment = request.RenovationInvestment,
                BalconyCount = request.BalconyCount ?? 0,
                HasTerrace = request.HasTerrace ?? false,
                HasGarage = request.HasGarage ?? false,
                HasParking = request.HasParking ?? false,
                HasSwimmingPool = request.HasSwimmingPool ?? false,
                IsFurnished = request.IsFurnished ?? false,
                HasAirConditioning = request.HasAirConditioning ?? false,
                HasSeaView = request.HasSeaView ?? false,
                HasCityView = request.HasCityView ?? false,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                EnergyCertificate = request.EnergyCertificate,
                Notes = request.Notes,
                PurchasePrice = request.PurchasePrice ?? 0,
                PurchaseDate = request.PurchaseDate ?? default
            };
        }

        public static OwnedPropertyResponse MapToResponse(OwnedProperty entity)
        {
            return new OwnedPropertyResponse
            {
                Id = entity.Id,
                Name = entity.Name,
                MarketAreaId = entity.MarketAreaId,
                PurchasePrice = entity.PurchasePrice,
                PurchaseDate = entity.PurchaseDate,
                PropertyType = entity.PropertyType,
                Typology = entity.Typology,
                AreaM2 = entity.AreaM2,
                Bathrooms = entity.Bathrooms ?? 0,
                Floor = entity.Floor,
                TotalFloors = entity.TotalFloors,
                HasElevator = entity.HasElevator,
                HasAirConditioning = entity.HasAirConditioning,
                Condition = entity.Condition,
                ConstructionYear = entity.ConstructionYear,
                RenovationYear = entity.RenovationYear,
                RenovationInvestment = entity.RenovationInvestment,
                BalconyCount = entity.BalconyCount,
                HasTerrace = entity.HasTerrace,
                HasGarage = entity.HasGarage,
                HasParking = entity.HasParking,
                HasSwimmingPool = entity.HasSwimmingPool,
                IsFurnished = entity.IsFurnished,
                HasSeaView = entity.HasSeaView,
                HasCityView = entity.HasCityView,
                EnergyCertificate = entity.EnergyCertificate,
                Notes = entity.Notes,
                DistanceToBeachMeters = entity.DistanceToBeachMeters,
            };

        }

        public static PremiumFeatureResponse MapToResponse(PremiumFeature entity)
        {
            var response = new PremiumFeatureResponse
            {
                Feature = entity.Feature,
                PremiumPercent = entity.PremiumPercent,
                CalculatedAtUtc = entity.CalculatedAtUtc,
            };

            return response;
        }
    }
}
