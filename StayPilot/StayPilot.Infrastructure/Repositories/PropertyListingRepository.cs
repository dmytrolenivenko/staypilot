
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;
using StayPilot.Infrastructure.Persistence;


namespace StayPilot.Infrastructure.Repositories
{
    /// <summary>
    /// Talks to the database for properties (read one, read by URL, add, and search).
    /// </summary>
    public class PropertyListingRepository : IPropertyListingRepository
    {
        private readonly StayPilotDbContext _context;

        public PropertyListingRepository(StayPilotDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Reads one property by its Id, with its market area and its snapshots.
        /// Returns null if no property has this Id.
        /// </summary>
        public async Task<PropertyListing?> GetPropertyListingByIdAsync(int id)
        {
            var property = await _context.PropertyListings
                .Include(x => x.MarketArea)       // also load the market area
                .Include(x => x.ListingSnapshots) // also load the snapshots
                .FirstOrDefaultAsync(x => x.Id == id);

            return property;
        }

        /// <summary>
        /// Reads one property by its source URL, with its market area and its snapshots.
        /// Used to check if a property is already saved. Returns null if not found.
        /// </summary>
        public async Task<PropertyListing?> GetPropertyListingByUrlAsync(string url)
        {
            return await _context.PropertyListings
                .Include (x => x.MarketArea)
                .Include (x => x.ListingSnapshots)
                .FirstOrDefaultAsync(x => x.SourceUrl == url);
        }

        /// <summary>
        /// Adds a new property. It is only kept in memory here;
        /// SaveChangesAsync writes it to the database.
        /// </summary>
        public async Task<PropertyListing> AddPropertyListingAsync(PropertyListing propertyListing)
        {
            var entry = await _context.PropertyListings.AddAsync(propertyListing);
            return entry.Entity;
        }

        /// <summary>
        /// Writes all pending changes to the database.
        /// </summary>
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Searches properties using the filters in the request, one page at a time.
        /// Returns the properties for the page and the total count of matches.
        /// </summary>
        public async Task<(List<PropertyListing> Items, int TotalRecords)> FilterPropertyAsync(FilterPropertyListingRequest request)
        {
            // Start with all properties. We add one filter below for each value the caller sent.
            var query = _context.PropertyListings.AsQueryable();

            #region Filters

            if (!string.IsNullOrWhiteSpace(request.District))
            {
                query = query.Where(x => x.MarketArea.District == request.District);
            }

            if (!string.IsNullOrWhiteSpace(request.Municipality))
            {
                query = query.Where(x => x.MarketArea.Municipality == request.Municipality);
            }

            if (!string.IsNullOrWhiteSpace(request.Town))
            {
                query = query.Where(x => x.MarketArea.Town == request.Town);
            }

            if (!string.IsNullOrWhiteSpace(request.Zone))
            {
                query = query.Where(x => x.MarketArea.Zone == request.Zone);
            }

            if (request.PropertyType is not null)
            {
                query = query.Where(x => x.PropertyType == request.PropertyType);
            }

            if (request.Typology is not null)
            {
                query = query.Where(x => x.Typology == request.Typology);
            }

            if (request.MinAreaM2 is not null)
            {
                query = query.Where(x => x.AreaM2 >= request.MinAreaM2);
            }

            if (request.MaxAreaM2 is not null)
            {
                query = query.Where(x => x.AreaM2 <= request.MaxAreaM2);
            }

            if (request.MarketAreaId is not null)
            {
                query = query.Where(x => x.MarketAreaId == request.MarketAreaId);
            }

            if (request.Bathrooms is not null)
            {
                query = query.Where(x => x.Bathrooms == request.Bathrooms);
            }

            if (request.Floor is not null)
            {
                query = query.Where(x => x.Floor == request.Floor);
            }

            if (request.TotalFloors is not null)
            {
                query = query.Where(x => x.TotalFloors == request.TotalFloors);
            }

            if (request.HasElevator is not null)
            {
                query = query.Where(x => x.HasElevator == request.HasElevator);
            }

            if (request.HasAirConditioning is not null)
            {
                query = query.Where(x => x.HasAirConditioning == request.HasAirConditioning);
            }

            if (request.Condition is not null)
            {
                query = query.Where(x => x.Condition == request.Condition);
            }

            if (request.ConstructionYear is not null)
            {
                query = query.Where(x => x.ConstructionYear == request.ConstructionYear);
            }

            if (request.RenovationYear is not null)
            {
                query = query.Where(x => x.RenovationYear == request.RenovationYear);
            }

            if (request.BalconyCount is not null)
            {
                query = query.Where(x => x.BalconyCount == request.BalconyCount);
            }

            if (request.HasTerrace is not null)
            {
                query = query.Where(x => x.HasTerrace == request.HasTerrace);
            }

            if (request.HasGarage is not null)
            {
                query = query.Where(x => x.HasGarage == request.HasGarage);
            }

            if (request.HasParking is not null)
            {
                query = query.Where(x => x.HasParking == request.HasParking);
            }

            if (request.HasSwimmingPool is not null)
            {
                query = query.Where(x => x.HasSwimmingPool == request.HasSwimmingPool);
            }

            if (request.IsFurnished is not null)
            {
                query = query.Where(x => x.IsFurnished == request.IsFurnished);
            }

            if (request.HasSeaView is not null)
            {
                query = query.Where(x => x.HasSeaView == request.HasSeaView);
            }

            if (request.HasCityView is not null)
            {
                query = query.Where(x => x.HasCityView == request.HasCityView);
            }

            // Keep only properties whose beach is close enough (distance under the asked limit).
            if (request.DistanceToBeachMeters is not null)
            {
                query = query.Where(x => x.DistanceToBeachMeters <= request.DistanceToBeachMeters);
            }

            // Price and status live on the snapshot, not the property.
            // So each filter below looks at the NEWEST snapshot (sorted by date, first one).

            if (request.MaxPrice is not null)
            {
                query = query.Where(x => x.ListingSnapshots.OrderByDescending(s => s.SnapshotDateUtc).FirstOrDefault()!.Price <= request.MaxPrice);
            }

            if (request.MinPrice is not null)
            {
                query = query.Where(x => x.ListingSnapshots.OrderByDescending(s => s.SnapshotDateUtc).FirstOrDefault()!.Price >= request.MinPrice);
            }

            if (request.MinPricePerM2 is not null)
            {
                query = query.Where(x => x.ListingSnapshots.OrderByDescending(s => s.SnapshotDateUtc).FirstOrDefault()!.PricePerM2 >= request.MinPricePerM2);
            }

            if (request.MaxPricePerM2 is not null)
            {
                query = query.Where(x => x.ListingSnapshots.OrderByDescending(s => s.SnapshotDateUtc).FirstOrDefault()!.PricePerM2 <= request.MaxPricePerM2);
            }

            if (request.ListingStatus is not null)
            {
                query = query.Where(x => x.ListingSnapshots.OrderByDescending(s => s.SnapshotDateUtc).FirstOrDefault()!.Status == request.ListingStatus);
            }

            #endregion

            // Count all matches BEFORE paging, so the caller knows the real total.
            var totalResults = await query.CountAsync();

            // Order the results. The price sorts use the newest snapshot, same as the filters above.
            query = request.SortBy switch
            {
                SortBy.Price => request.SortDescending
                ? query.OrderByDescending(x => x.ListingSnapshots.OrderByDescending(s => s.SnapshotDateUtc).FirstOrDefault()!.Price)
                : query.OrderBy(x => x.ListingSnapshots.OrderByDescending(s => s.SnapshotDateUtc).FirstOrDefault()!.Price),

                SortBy.PricePerM2 => request.SortDescending
                ? query.OrderByDescending(x => x.ListingSnapshots.OrderByDescending(s => s.SnapshotDateUtc).FirstOrDefault()!.PricePerM2)
                : query.OrderBy(x => x.ListingSnapshots.OrderByDescending(s => s.SnapshotDateUtc).FirstOrDefault()!.PricePerM2),

                SortBy.AreaM2 => request.SortDescending
                ? query.OrderByDescending(x => x.AreaM2)
                : query.OrderBy(x => x.AreaM2),

                SortBy.CreatedAtUtc => request.SortDescending
                ? query.OrderByDescending(x => x.CreatedAtUtc)
                : query.OrderBy(x => x.CreatedAtUtc),

                SortBy.DistanceToBeachMeters => request.SortDescending
                ? query.OrderByDescending(x => x.DistanceToBeachMeters)
                : query.OrderBy(x => x.DistanceToBeachMeters),

                SortBy.Id => request.SortDescending
                ? query.OrderByDescending(x => x.Id)
                : query.OrderBy(x => x.Id),

                // No sort asked (or unknown) -> sort by Id.
                _ => query.OrderBy(x => x.Id)
            };

            var items = await query
                .Include(x => x.MarketArea) // also load the market area
                // Also load only the newest snapshot of each property (we do not need the older ones).
                .Include(x => x.ListingSnapshots.OrderByDescending(s => s.SnapshotDateUtc).Take(1))
                .Skip((request.PageNumber - 1) * request.PageSize) // jump over the earlier pages
                .Take(request.PageSize)                            // take only this page
                .ToListAsync();

            return (items, totalResults);
        }

        /// <summary>
        /// Finds properties that can be compared to the given one: same property type,
        /// a room layout within one step (a T2 is a fair comp for a T1), and either in the
        /// same market area or within radiusMeters of the given lat/lon. Only keeps a
        /// listing if its newest snapshot is not older than the cutoff.
        /// Returns at most 100, best first: same market area, then nearest.
        /// When the property has no coordinates it falls back to the market area alone.
        /// </summary>
        public async Task<List<PropertyListing>> GetComparablePropertyListingAsync(int marketId, PropertyType propertyType, Typology typology, int areaM2, decimal? latitude, decimal? longitude, int radiusMeters, int months)
        {
            var query = _context.PropertyListings.AsQueryable();

            // Same kind of property (apartment, villa, and so on).
            query = query.Where(x => x.PropertyType == propertyType);

            // Same room layout +-1 (a T2 is a fair comp for a T1). Asking for the exact
            // typology throws away nearly every nearby listing, and one bedroom either way
            // barely moves the price per m2 - which is what the estimate is built on.
            query = query.Where(x => x.Typology == typology || x.Typology == typology - 1 || x.Typology == typology + 1);

            // Only keep it if its newest snapshot is not older than the cutoff.
            query = query.Where(x => x.ListingSnapshots.OrderByDescending(s => s.SnapshotDateUtc).Select(s => s.SnapshotDateUtc).FirstOrDefault() >= DateTime.UtcNow.AddMonths(- months));

            // To avoid corrupted data, do not return tiny areasM2 (30 m2)
            query = query.Where(x => x.AreaM2 > 30);

            const int maxComps = 100;

            IOrderedQueryable<PropertyListing> ordered;

            if (latitude.HasValue && longitude.HasValue)
            {
                // The database stores degrees but the caller asks in metres, so the radius
                // has to be converted first. One degree of latitude is always about 111320 m.
                // Degrees of longitude get shorter the further you are from the equator, so
                // they are scaled by cos(latitude) - in the Algarve (37N) a degree of
                // longitude is about 89 km, not 111 km.
                const double metersPerDegree = 111_320;

                var lat = latitude.Value;
                var lon = longitude.Value;
                var lonScale = (decimal)Math.Cos((double)lat * Math.PI / 180);
                var radiusDegrees = (decimal)(radiusMeters / metersPerDegree);
                var radiusDegreesSquared = radiusDegrees * radiusDegrees;

                // Same market area, or close enough on the map. Distances are kept squared
                // so there is no square root to take - it does not change the ordering.
                query = query.Where(x => x.MarketAreaId == marketId
                    || (x.Latitude - lat) * (x.Latitude - lat)
                     + (x.Longitude - lon) * lonScale * (x.Longitude - lon) * lonScale <= radiusDegreesSquared);

                // Own market area first, because a zone 800 m away can be a completely
                // different market (a beachfront zone against an old town). Only then the
                // nearest ones, so a comp next door beats one at the edge of the circle.
                // This distance must stay identical to the one used in the filter above.
                ordered = query
                    .OrderByDescending(x => x.MarketAreaId == marketId)
                    .ThenBy(x => (x.Latitude - lat) * (x.Latitude - lat)
                               + (x.Longitude - lon) * lonScale * (x.Longitude - lon) * lonScale);
            }
            else
            {
                // No coordinates on the property, so a radius means nothing here. Fall back
                // to the market area on its own, closest in size first.
                query = query.Where(x => x.MarketAreaId == marketId);

                ordered = query.OrderBy(x => Math.Abs(x.AreaM2 - areaM2));
            }

            // ThenBy(Id) keeps the "top N" deterministic when several comps tie.
            var items = await ordered
                .ThenBy(x => x.Id)
                .Take(maxComps)
                .Include(x => x.MarketArea)
                .Include(x => x.ListingSnapshots.OrderByDescending(s => s.SnapshotDateUtc).Take(1))
                .ToListAsync();

            return items;
        }

        /// <summary>
        /// Gets every property listing, with just its newest snapshot loaded.
        /// Used by the feature-premium calculation, which groups by Typology across
        /// the whole dataset — not one market area or a paged slice of it.
        /// </summary>
        public async Task<List<PropertyListing>> GetAllListingsForFeaturePremiumCalculationAsync()
        {
            return await _context.PropertyListings
                .Include(x => x.MarketArea) // needed so the premium calc can filter by town name
                .Include(x => x.ListingSnapshots.OrderByDescending(s => s.SnapshotDateUtc).Take(1))
                .ToListAsync();
        }
    }
}
