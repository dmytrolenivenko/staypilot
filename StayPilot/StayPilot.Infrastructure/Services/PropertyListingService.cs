
using Microsoft.EntityFrameworkCore;
using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Domain.Entities;
using System.Globalization;
using System.Text;
using StayPilot.Application.Helpers.Mappers;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Application.Interfaces.Services;

namespace StayPilot.Infrastructure.Services
{
    public class PropertyListingService : IPropertyListingService
    {
        private readonly IPropertyListingRepository _propertyListingRepo;
        private readonly IMarketAreaRepository _marketAreaRepo;
        private readonly IBeachMarkerRepository _beachMarkerRepo;
        private readonly IListingSnapshotRepository _listingSnapshotRepo;

        public PropertyListingService(IPropertyListingRepository propertyListingRepo, IMarketAreaRepository marketAreaRepo, IBeachMarkerRepository beachMarkerRepo, IListingSnapshotRepository listingSnapshotRepo)
        {
            _propertyListingRepo = propertyListingRepo;
            _marketAreaRepo = marketAreaRepo;
            _beachMarkerRepo = beachMarkerRepo;
            _listingSnapshotRepo = listingSnapshotRepo;
        }

        /// <summary>
        /// Controller method to get a property listing by its ID.
        /// </summary>
        /// <param name="propertyId"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<PropertyListingResponse?> GetPropertyListingByIdAsync(int propertyId)
        {
            var property = await _propertyListingRepo.GetPropertyListingByIdAsync(propertyId);

            var listing = await _listingSnapshotRepo.GetListingSnapshotByPropertyIdAsync(propertyId);

            if (property == null)
            {
                return null;
            }

            return Converter.MapToResponse(property, listing);
        }


        /// <summary>
        /// Controller method to add a new property listing.
        /// </summary>
        /// <param name="propertyListing"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        
        public async Task<PropertyListingResponse> AddPropertyListingAsync(PropertyListingRequest propertyListing)
        {
                // Creating repos for helpers
                var marketAreasRepo = await _marketAreaRepo.GetAllMarketAreasAsync();
                var beachesRepo = await _beachMarkerRepo.GetAllBeachMarkersAsync();

                // Firstly, check if the property listing already exists based on the SourceUrl
                var propertyExist = await _propertyListingRepo.GetPropertyListingByUrlAsync(propertyListing.SourceUrl);

                if (propertyExist != null)
                {
                    return Converter.MapToResponse(propertyExist);
                }

                // Map the request to the entity and set the MarketAreaId
                var property = Converter.MapToEntity(propertyListing);
                property.MarketAreaId = propertyListing.MarketAreaId ?? GetMarketId(marketAreasRepo, propertyListing);
                property.MarketArea = marketAreasRepo.FirstOrDefault(x => x.Id == property.MarketAreaId) ?? throw new InvalidOperationException("MarketArea can not be null");

                // Check the Lat and Lon and throw if absent
                if (propertyListing.Latitude == null || propertyListing.Longitude == null)
                {
                    throw new InvalidOperationException("Latitude and Longitude must be provided for the property listing.");
                }

                // Get the closest beach to the property listing
                var closesBeach = GetTheClosestBeach(beachesRepo, propertyListing);

                // If a closest beach is found, calculate the distance and set the relevant properties
                if (closesBeach is not null)
                {
                    var distanceToBeachMeters = CalculateDistanceMeters((double)propertyListing.Latitude.Value, (double)propertyListing.Longitude.Value, (double)closesBeach.Latitude, (double)closesBeach.Longitude);

                    property.NearestBeachName = closesBeach.Name;
                    property.NearestBeachMarkerId = closesBeach.Id;
                    property.DistanceToBeachMeters = (int)distanceToBeachMeters;
                    property.DistanceToBeachMethod = "osm_center_point";
                }

                // Create the ListingSnapshot from the request
                var listingSnapshot = Converter.MapToEntity(propertyListing.ListingSnapshot);
                listingSnapshot.PropertyListing = property;

            await _propertyListingRepo.AddPropertyListingAsync(property);
            await _listingSnapshotRepo.AddListingSnapshotAsync(listingSnapshot);
            await _propertyListingRepo.SaveChangesAsync();

            return Converter.MapToResponse(property, listingSnapshot);
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

        private static int GetMarketId(List<MarketArea> marketAreas, PropertyListingRequest propertyListing)
        {
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

            // Scraped listings often can't tell which Zone a Town falls under — Idealista's page
            // doesn't expose that level of detail, so Zone frequently arrives null. Rather than
            // rejecting the whole listing over a missing Zone, fall back to any MarketArea for
            // the same Town, preferring one with no Zone of its own.
            marketArea ??= marketAreas
                .Where(x =>
                    NormalizeText(x.Country) == country &&
                    NormalizeText(x.District) == district &&
                    NormalizeText(x.Municipality) == municipality &&
                    NormalizeText(x.Town) == town)
                .OrderBy(x => x.Zone == null ? 0 : 1)
                .ThenBy(x => x.Id)
                .FirstOrDefault();

            // Some captures can only tell us the Municipality, not the specific parish (e.g.
            // Idealista's page just says "Lagoa" with no further breakdown) — Town won't match
            // any seed row either in that case. Rather than reject the listing outright, land it
            // on some MarketArea within the right Municipality so it's at least grouped
            // correctly at that level; it can be corrected to the right parish/zone by hand later.
            marketArea ??= marketAreas
                .Where(x =>
                    NormalizeText(x.Country) == country &&
                    NormalizeText(x.District) == district &&
                    NormalizeText(x.Municipality) == municipality)
                .OrderBy(x => x.Zone == null ? 0 : 1)
                .ThenBy(x => x.Id)
                .FirstOrDefault();

            if (marketArea == null)
                throw new InvalidOperationException("Market area not found. Property URL: " + propertyListing.SourceUrl);

            return marketArea.Id;
        }

        private static BeachMarker? GetTheClosestBeach(List<BeachMarker> beaches, PropertyListingRequest propertyListing)
        {
            if (propertyListing.Latitude == null || propertyListing.Longitude == null)
            {
                return null;
            }

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
