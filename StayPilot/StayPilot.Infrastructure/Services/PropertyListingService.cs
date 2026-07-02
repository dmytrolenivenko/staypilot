
using Microsoft.EntityFrameworkCore;
using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Interfaces;
using StayPilot.Domain.Entities;
using StayPilot.Infrastructure.Persistence;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Text;

namespace StayPilot.Infrastructure.Services
{
    public class PropertyListingService : IPropertyListingService
    {
        private readonly StayPilotDbContext _context;

        public PropertyListingService(StayPilotDbContext context)
        {
            _context = context;
        }

        public async Task<PropertyListingResponse> AddPropertyListingAsync(PropertyListingRequest propertyListing)
        {
            var marketAreaId = await GetMarketId(_context, propertyListing);

            var closesBeach = await GetTheClosestBeach(_context, propertyListing);

            var property = MapToEntity(propertyListing);
            property.MarketAreaId = marketAreaId;

            if (closesBeach is not null)
            {
                var distanceToBeachMeters = CalculateDistanceMeters(
                    (double)propertyListing.Latitude.Value, 
                    (double)propertyListing.Longitude.Value, 
                    (double)closesBeach.Latitude, 
                    (double)closesBeach.Longitude);

                property.NearestBeachName = closesBeach.Name;
                property.NearestBeachMarkerId = closesBeach.Id;
                property.DistanceToBeachMeters = (int)distanceToBeachMeters;
                property.DistanceToBeachMethod = "Haversine";
            } 

            _context.PropertyListings.Add(property);
            await _context.SaveChangesAsync();
            await _context.Entry(property).Reference(p => p.MarketArea).LoadAsync();
            return MapToResponse(property);
        }

        public async Task<PropertyListingResponse?> GetPropertyListingByIdAsync(int propertyId)
        {
            var property = await _context.PropertyListings
                .Include(x => x.MarketArea)
                .FirstOrDefaultAsync(x => x.Id == propertyId);

            if (property == null)
            {
                return null;
            }

            return MapToResponse(property);
        }

        private PropertyListingResponse MapToResponse(PropertyListing property)
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
                EnergyCertificate = property.EnergyCertificate
            };
        }

        private PropertyListing MapToEntity(PropertyListingRequest property)
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
                EnergyCertificate = property.EnergyCertificate
            };
        }

        private static string NormalizeText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);

            var builder = new StringBuilder();

            foreach (var c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);

                if (category != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(c);
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        private async Task<int> GetMarketId(StayPilotDbContext _context, PropertyListingRequest propertyListing)
        {
            var marketAreas = await _context.MarketAreas.ToListAsync();

            var country = NormalizeText(propertyListing.Country);
            var district = NormalizeText(propertyListing.District);
            var municipality = NormalizeText(propertyListing.Municipality);
            var town = NormalizeText(propertyListing.Town);
            var zone = NormalizeText(propertyListing.Zone ?? string.Empty);

            var marketArea = marketAreas.FirstOrDefault(x =>
                NormalizeText(x.Country) == country &&
                NormalizeText(x.District) == district &&
                NormalizeText(x.Municipality) == municipality &&
                NormalizeText(x.Town) == town &&
                NormalizeText(x.Zone ?? string.Empty) == zone);

            if (marketArea == null)
            {
                throw new InvalidOperationException("Market area not found.");
            }

            return marketArea.Id;
        }

        private static async Task<BeachMarker?> GetTheClosestBeach(StayPilotDbContext _context, PropertyListingRequest propertyListing)
        {
            if (propertyListing.Latitude == null || propertyListing.Longitude == null)
            {
                return null;
            }

            var beaches = await _context.BeachMarkers.ToListAsync();

            var propertyLat = (double)propertyListing.Latitude.Value;
            var propertyLon = (double)propertyListing.Longitude.Value;

            var closestBeach = beaches
                .OrderBy(beach => CalculateDistanceMeters(
                    propertyLat,
                    propertyLon,
                    (double)beach.Latitude,
                    (double)beach.Longitude))
                .FirstOrDefault();

            return closestBeach;
        }
        private static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadiusMeters = 6371000;

            double ToRadians(double degrees)
            {
                return degrees * Math.PI / 180;
            }

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) *
                Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) *
                Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusMeters * c;
        }

    }
}
