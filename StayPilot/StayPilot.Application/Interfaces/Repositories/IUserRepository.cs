using StayPilot.Domain.Entities;

namespace StayPilot.Application.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByExternalIdAsync(string externalId);

        Task<User?> CreateAsync(User entity);

        Task SaveChangesAsync();
    }
}
