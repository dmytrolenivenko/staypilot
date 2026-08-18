
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
        public async Task<List<PropertyListing>?> GetBulkPropertyListingByUrlAsync(List<string> urls)
        {
            return await _context.PropertyListings
                .Include (x => x.MarketArea)
                .Include (x => x.ListingSnapshots)
                .Where(x => urls.Contains(x.SourceUrl))
                .ToListAsync();
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
        /// Turns a blank filter into null, so "not chosen" is one value and not three.
        /// </summary>
        private static string? NullIfBlank(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        /// <summary>
        /// Works out the middle point of a place's listings, to use as the centre of a radius
        /// search. Returns null when no radius was asked for, when no place was chosen (a
        /// circle needs a centre), or when nothing advertised there carries coordinates —
        /// in every one of those cases the caller falls back to matching the place exactly.
        /// The middle point of the adverts, not of the place: we hold no real borders, which
        /// is the same approximation the neighbour gaps screen is built on.
        /// </summary>
        private async Task<(decimal Latitude, decimal Longitude)?> GetPlaceCentreAsync(string? district, string? municipality, string? town, string? zone, double? withinKm)
        {
            if (withinKm is not > 0)
            {
                return null;
            }

            if (district is null && municipality is null && town is null && zone is null)
            {
                return null;
            }

            var placed = _context.PropertyListings.Where(x => x.Latitude != null && x.Longitude != null);

            if (district is not null)
            {
                placed = placed.Where(x => x.MarketArea.District == district);
            }

            if (municipality is not null)
            {
                placed = placed.Where(x => x.MarketArea.Municipality == municipality);
            }

            if (town is not null)
            {
                placed = placed.Where(x => x.MarketArea.Town == town);
            }

            if (zone is not null)
            {
                placed = placed.Where(x => x.MarketArea.Zone == zone);
            }

            // Averaging a nullable column gives null back on an empty set instead of throwing.
            var latitude = await placed.AverageAsync(x => x.Latitude);
            var longitude = await placed.AverageAsync(x => x.Longitude);

            if (latitude is null || longitude is null)
            {
                return null;
            }

            return (latitude.Value, longitude.Value);
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

            // Blank means "not chosen", and null reads better than "" in the query below.
            var district = NullIfBlank(request.District);
            var municipality = NullIfBlank(request.Municipality);
            var town = NullIfBlank(request.Town);
            var zone = NullIfBlank(request.Zone);

            // With a radius the place stops being a border and becomes a circle around it,
            // so a search for Lisboa also brings back Oeiras and Amadora. Without one, the
            // place is matched exactly, level by level, as it always was.
            var centre = await GetPlaceCentreAsync(district, municipality, town, zone, request.WithinKm);

            if (centre is null)
            {
                if (district is not null)
                {
                    query = query.Where(x => x.MarketArea.District == district);
                }

                if (municipality is not null)
                {
                    query = query.Where(x => x.MarketArea.Municipality == municipality);
                }

                if (town is not null)
                {
                    query = query.Where(x => x.MarketArea.Town == town);
                }

                if (zone is not null)
                {
                    query = query.Where(x => x.MarketArea.Zone == zone);
                }
            }
            else
            {
                var latitude = centre.Value.Latitude;
                var longitude = centre.Value.Longitude;

                // Degrees, not metres, because that is what the columns hold. One degree of
                // latitude is always about 111320 m; degrees of longitude get shorter away
                // from the equator, so they are scaled by cos(latitude). Same conversion as
                // the comparables query - keep the two in step if either ever changes.
                const double metersPerDegree = 111_320;

                var longitudeScale = (decimal)Math.Cos((double)latitude * Math.PI / 180);
                var radiusDegrees = (decimal)(request.WithinKm!.Value * 1000 / metersPerDegree);
                var radiusDegreesSquared = radiusDegrees * radiusDegrees;

                // Inside the place, or close enough to it on the map. Distances stay squared
                // so there is no square root to take; it does not change what falls inside.
                // The place half also keeps listings with no coordinates, which a circle
                // alone would silently drop.
                query = query.Where(x =>
                    ((district == null || x.MarketArea.District == district)
                        && (municipality == null || x.MarketArea.Municipality == municipality)
                        && (town == null || x.MarketArea.Town == town)
                        && (zone == null || x.MarketArea.Zone == zone))
                    || (x.Latitude - latitude) * (x.Latitude - latitude)
                     + (x.Longitude - longitude) * longitudeScale * (x.Longitude - longitude) * longitudeScale <= radiusDegreesSquared);
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
        /// a room layout within one step (a T2 is a fair comp for a T1), a floor area within
        /// a quarter either way, and either in the same market area or within radiusMeters of
        /// the given lat/lon. Only keeps a listing if its newest snapshot is not older than
        /// the cutoff. Ordered best first: same market area, then nearest.
        /// When the property has no coordinates it falls back to the market area alone.
        /// </summary>
        public async Task<List<PropertyListing>> GetComparablePropertyListingAsync(int marketId, PropertyType propertyType, Typology typology, int areaM2, int? distanceToBeachMeters, decimal? latitude, decimal? longitude, int radiusMeters, int months)
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

            // Within a quarter of this property's size. Price per m2 falls steadily as flats get
            // bigger, so an unbanded set answers a different question than the one asked: a 45 m2
            // flat was being compared against 60-90 m2 ones, and their EUR/m2 is not its EUR/m2.
            // The band is what makes "median comp EUR/m2" a number worth putting on screen.
            const double areaBand = 0.25;

            var smallestComparableArea = (int)Math.Floor(areaM2 * (1 - areaBand));
            var largestComparableArea = (int)Math.Ceiling(areaM2 * (1 + areaBand));

            query = query.Where(x => x.AreaM2 >= smallestComparableArea && x.AreaM2 <= largestComparableArea);

            // Within a factor of two of this property's distance to the beach. In a beach town the
            // radius alone is not comparability: measured around one Quarteira flat, 65-92 m2 flats
            // inside the same 2km circle asked EUR 8,223/m2 within 300m of the sand and EUR 5,619
            // past 800m. Comparing a flat 615m out against the seafront is how "comps alone" came
            // back at EUR 533,000 for a 73 m2 T2. Skipped when the distance is unknown - we cannot
            // band on something we never measured.
            if (distanceToBeachMeters is > 0)
            {
                var nearestComparableBeach = distanceToBeachMeters.Value / 2;
                var farthestComparableBeach = distanceToBeachMeters.Value * 2;

                query = query.Where(x => x.DistanceToBeachMeters >= nearestComparableBeach
                                      && x.DistanceToBeachMeters <= farthestComparableBeach);
            }

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

            // Every comp that fits, not a top slice of them: the old cap of 100 quietly decided
            // WHICH comps counted, because the ordering puts the whole market area ahead of
            // anything outside it. The band above is what keeps this set small enough to be
            // worth returning in full. ThenBy(Id) only settles ties, so the order is stable.
            var items = await ordered
                .ThenBy(x => x.Id)
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

        /// <summary>
        /// Reads one slice of the market for the overview screen: a place, optionally narrowed to
        /// one property type and one room layout, with just the newest snapshot of each listing.
        ///
        /// The whole slice, not a page of it - the caller takes medians and a distribution over it.
        /// The market area is filtered on but not loaded: the overview counts and prices listings,
        /// it never prints their address, so pulling the area rows would be paid for nothing.
        /// </summary>
        public async Task<List<PropertyListing>> GetListingsForMarketOverviewAsync(string? district, string? municipality, string? town, PropertyType? propertyType, Typology? typology)
        {
            var query = _context.PropertyListings.AsQueryable();

            if (!string.IsNullOrWhiteSpace(district))
            {
                query = query.Where(x => x.MarketArea.District == district);
            }

            if (!string.IsNullOrWhiteSpace(municipality))
            {
                query = query.Where(x => x.MarketArea.Municipality == municipality);
            }

            if (!string.IsNullOrWhiteSpace(town))
            {
                query = query.Where(x => x.MarketArea.Town == town);
            }

            if (propertyType is not null)
            {
                query = query.Where(x => x.PropertyType == propertyType);
            }

            if (typology is not null)
            {
                query = query.Where(x => x.Typology == typology);
            }

            return await query
                // The area comes along because the overview now also breaks the slice into the
                // places inside it, and that needs a district/município/freguesia per listing.
                .Include(x => x.MarketArea)
                .Include(x => x.ListingSnapshots.OrderByDescending(s => s.SnapshotDateUtc).Take(1))
                .ToListAsync();
        }

        /// <summary>
        /// Reads one place with the full price history of every listing in it.
        ///
        /// Deliberately unpaged and unfiltered beyond the place: the two things this feeds -
        /// how long homes sit, and which way prices are moving - are both properties of the
        /// whole place, and a page of twenty would describe the page.
        /// </summary>
        public async Task<List<PropertyListing>> GetListingsWithHistoryAsync(string? district, string? municipality, string? town)
        {
            var query = _context.PropertyListings.AsQueryable();

            if (!string.IsNullOrWhiteSpace(district))
            {
                query = query.Where(x => x.MarketArea.District == district);
            }

            if (!string.IsNullOrWhiteSpace(municipality))
            {
                query = query.Where(x => x.MarketArea.Municipality == municipality);
            }

            if (!string.IsNullOrWhiteSpace(town))
            {
                query = query.Where(x => x.MarketArea.Town == town);
            }

            return await query
                // Every snapshot, not just the newest - see the interface for why.
                .Include(x => x.ListingSnapshots.OrderByDescending(s => s.SnapshotDateUtc))
                .ToListAsync();
        }
    }
}
