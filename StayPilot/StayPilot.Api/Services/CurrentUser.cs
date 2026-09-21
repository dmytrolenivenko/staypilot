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
            var claimUser = _httpContextAccessor.HttpContext?.User;

            var externalId = claimUser?.GetObjectId();

            // No oid means nobody is signed in, which can only happen if an action that reaches
            // this forgot its [Authorize]. Refuse rather than carry on: the old code went
            // straight to the insert below and wrote a User row with a null ExternalId - a
            // column that is NOT NULL and uniquely indexed, so the first anonymous caller broke
            // the table for the next one.
            if (claimUser is null || string.IsNullOrEmpty(externalId))
            {
                throw new UnauthorizedAccessException(
                    "No signed-in user on this request - the action is missing [Authorize].");
            }

            var existing = await _userRepository.GetByExternalIdAsync(externalId);
            if (existing is not null)
            {
                return existing.Id;
            }

            // First time we see this Entra user - create their User row now.
            var newUser = new User
            {
                ExternalId = externalId,
                UserEmail = EmailOf(claimUser),
                UserName = claimUser.GetDisplayName() ?? string.Empty,
            };

            // The nullable return is loose typing on the interface - EF's AddAsync always hands
            // the entity back - but the compiler cannot know that, and it is the same instance
            // either way, so fall back to the one we just built.
            var created = await _userRepository.CreateAsync(newUser) ?? newUser;

            await _userRepository.SaveChangesAsync();

            return created.Id;
        }

        /// <summary>
        /// The signed-in account's email address, or an empty string when the token carries none.
        /// </summary>
        /// <remarks>
        /// Two claims have to be tried. Workforce accounts put the address in
        /// <c>preferred_username</c>; the CIAM local email+password accounts this app actually
        /// signs up do not - they put it in the <c>emails</c> array instead. Reading only the
        /// first one meant every local account fell back to an empty string, and UserEmail is
        /// uniquely indexed: the first such user got in and the second one collided with them
        /// and could never sign up. The Angular side has always tried both claims
        /// (app.component.ts); this is the same fallback on the server.
        /// </remarks>
        private static string EmailOf(ClaimsPrincipal claimUser)
        {
            return claimUser.FindFirstValue("preferred_username")
                ?? claimUser.FindFirstValue("emails")
                ?? string.Empty;
        }
    }
}
