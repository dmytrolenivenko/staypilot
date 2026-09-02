using Microsoft.EntityFrameworkCore;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Domain.Entities;
using StayPilot.Infrastructure.Persistence;

namespace StayPilot.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly StayPilotDbContext _context;

        public UserRepository(StayPilotDbContext context)
        {
            _context = context;
        }

        public async Task<User?> CreateAsync(User entity)
        {
            var user = await _context.AddAsync(entity);
            return user.Entity;
        }

        public async Task<User?> GetByExternalIdAsync(string externalId)
        {
            return await _context.Users.FirstOrDefaultAsync(x => x.ExternalId == externalId);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
