using Microsoft.Identity.Web;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Application.Interfaces.Services;
using StayPilot.Domain.Entities;
using System.Security.Claims;

namespace StayPilot.Api.Services
{
    /// <summary>
    /// Reads who is logged in from the current request, and creates a User row
    /// the first time we see them (JIT provisioning).
    /// </summary>
    public class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserRepository _userRepository;

        public CurrentUser(IHttpContextAccessor httpContextAccessor, IUserRepository userRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _userRepository = userRepository;
        }

        /// <summary>
        /// Finds the logged in user by their Entra id, or creates them if this is their first request.
        /// </summary>
        public async Task<int> GetCurrentUserIdAsync()
        {
            var claimUser = _httpContextAccessor.HttpContext.User;
            var externalId = claimUser.GetObjectId();

            var existing = await _userRepository.GetByExternalIdAsync(externalId);
            if (existing is not null)
            {
                return existing.Id;
            }

            // First time we see this Entra user - create their User row now.
            var newUser = new User
            {
                ExternalId = externalId,
                UserEmail = claimUser.FindFirstValue("preferred_username") ?? string.Empty,
                UserName = claimUser.GetDisplayName() ?? string.Empty,
            };

            var created = await _userRepository.CreateAsync(newUser);
            await _userRepository.SaveChangesAsync();

            return created.Id;
        }
    }
}
