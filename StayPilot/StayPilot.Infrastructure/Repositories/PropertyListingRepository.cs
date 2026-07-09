using Microsoft.EntityFrameworkCore;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Domain.Entities;
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
        /// Repository method to get all property listings.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<IEnumerable<PropertyListing>> GetAllPropertyListingsAsync()
        {
            return await _context.PropertyListings.ToListAsync();
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
    }
}
