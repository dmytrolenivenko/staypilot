using Abp.Extensions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;
using StayPilot.Infrastructure.Persistence;


namespace StayPilot.Infrastructure.Repositories
{
    public class PropertyListingRepository : IPropertyListingRepository
    {
        private readonly StayPilotDbContext _context;

        public PropertyListingRepository(StayPilotDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Repository method to get a property listing by its ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<PropertyListing?> GetPropertyListingByIdAsync(int id)
        {
            var property = await _context.PropertyListings
                .Include(x => x.MarketArea)
                .Include(x => x.ListingSnapshots)
                .FirstOrDefaultAsync(x => x.Id == id);

            return property;
        }

        public async Task<PropertyListing?> GetPropertyListingByUrlAsync(string url)
        {
            return await _context.PropertyListings
                .Include (x => x.MarketArea)
                .Include (x => x.ListingSnapshots)
                .FirstOrDefaultAsync(x => x.SourceUrl == url);
        }

        /// <summary>
        /// Repository method to add a new property listing.
        /// </summary>
        /// <param name="propertyListing"></param>
        /// <returns></returns>
        public async Task<PropertyListing> AddPropertyListingAsync(PropertyListing propertyListing)
        {
            var entry = await _context.PropertyListings.AddAsync(propertyListing);
            return entry.Entity;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Repository to get Properties with requests
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        /// 
        public async Task<(List<PropertyListing> Items, int TotalRecords)> FilterPropertyAsync(ListPropertyListingRequest request)
        {
            var query = _context.PropertyListings.AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Location))
            {
                query = query.Where(x => 
                x.MarketArea.District.Contains(request.Location) ||
                x.MarketArea.Municipality.Contains(request.Location) ||
                x.MarketArea.Town.Contains(request.Location) ||
                (x.MarketArea.Zone != null && x.MarketArea.Zone.Contains(request.Location)));
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

            if (request.DistanceToBeachMeters is not null)
            {
                query = query.Where(x => x.DistanceToBeachMeters <= request.DistanceToBeachMeters);
            }

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

            var totalResults = await query.CountAsync();

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

                _ => query.OrderBy(x => x.Id)
            };

            var items = await query
                .Include(x => x.MarketArea)
                .Include(x => x.ListingSnapshots.OrderByDescending(s => s.SnapshotDateUtc).Take(1))
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return (items, totalResults);
        }
    }
}
