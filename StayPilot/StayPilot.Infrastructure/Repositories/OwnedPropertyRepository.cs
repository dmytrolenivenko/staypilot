using Microsoft.EntityFrameworkCore;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Domain.Entities;
using StayPilot.Infrastructure.Persistence;

namespace StayPilot.Infrastructure.Repositories
{
    public class OwnedPropertyRepository : IOwnedPropertyRepository
    {
        private StayPilotDbContext _context;

        public OwnedPropertyRepository(StayPilotDbContext context) 
        {
            _context = context;
        }

        public async Task<OwnedProperty> CreateOwnedPropertyAsync(OwnedProperty entity)
        {
            var entry = await _context.OwnedProperties.AddAsync(entity);

            return entry.Entity;
        }

        public async Task<OwnedProperty?> GetOwnedPropertyAsync(int id)
        {
            var entity = await _context.OwnedProperties.FirstOrDefaultAsync(x => x.Id == id);

            return entity;
        }

        public async Task<string?> DeleteOwnedPropertyAsync(int id)
        {
            var propertyToDelete = await _context.OwnedProperties.FindAsync(id);

            if (propertyToDelete is null) return null;

            _context.OwnedProperties.Remove(propertyToDelete);

            return propertyToDelete.Name;
        }

        public async Task<OwnedProperty?> UpdateOwnedPropertyAsync(OwnedProperty entity)
        {
            var updatedEntity = _context.OwnedProperties.Update(entity);

            return updatedEntity.Entity;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<List<OwnedProperty>> GetAllOwnedPropertyAsync()
        {
            return await _context.OwnedProperties.ToListAsync();
        }
    }
}
