using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web;
using StayPilot.Api.Services;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Domain.Entities;
using System.Security.Claims;

namespace StayPilot.UnitTests
{
    // Hand-written fake - this project has no Moq dependency. It enforces the two unique indexes
    // the real Users table carries (ExternalId and UserEmail), because those are exactly what
    // turned the bugs under test into dead accounts rather than harmless duplicates.
    file class FakeUserRepo : IUserRepository
    {
        private readonly List<User> _saved = new();
        private readonly List<User> _pending = new();

        public FakeUserRepo(params User[] existing) => _saved.AddRange(existing);

        public IReadOnlyList<User> Saved => _saved;

        public Task<User?> GetByExternalIdAsync(string externalId) =>
            Task.FromResult(_saved.FirstOrDefault(x => x.ExternalId == externalId));

        public Task<User?> CreateAsync(User entity)
        {
            entity.Id = _saved.Count + _pending.Count + 1;
            _pending.Add(entity);

            return Task.FromResult<User?>(entity);
        }

        public Task SaveChangesAsync()
        {
            foreach (var user in _pending)
            {
                // What SQL Server would raise on IX_Users_ExternalId / IX_Users_UserEmail.
                if (_saved.Any(x => x.ExternalId == user.ExternalId))
                {
                    throw new InvalidOperationException($"Duplicate ExternalId '{user.ExternalId}'.");
                }

                if (_saved.Any(x => x.UserEmail == user.UserEmail))
                {
                    throw new InvalidOperationException($"Duplicate UserEmail '{user.UserEmail}'.");
                }

                _saved.Add(user);
            }

            _pending.Clear();

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// CurrentUser turns the token on the request into a User row, creating one the first time it
    /// sees an account. Two things it has to get right, neither of which was covered before:
    /// it must refuse a request with no signed-in account instead of provisioning a blank user,
    /// and it must find the email address wherever the tenant happens to put it.
    /// </summary>
    public class CurrentUserTests
    {
        private static CurrentUser ServiceFor(ClaimsPrincipal? principal, IUserRepository users)
        {
            var accessor = new HttpContextAccessor();

            if (principal is not null)
            {
                accessor.HttpContext = new DefaultHttpContext { User = principal };
            }

            return new CurrentUser(accessor, users);
        }

        // A workforce-style token: the address sits in preferred_username.
        private static ClaimsPrincipal WorkforceToken(string objectId, string preferredUsername) =>
            new(new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimConstants.Oid, objectId),
                    new Claim("preferred_username", preferredUsername),
                    new Claim("name", "Test Person"),
                },
                authenticationType: "Bearer"));

        // A CIAM local email+password token: no preferred_username at all, the address is in emails.
        private static ClaimsPrincipal CiamLocalToken(string objectId, string email) =>
            new(new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimConstants.Oid, objectId),
                    new Claim("emails", email),
                    new Claim("name", "Test Person"),
                },
                authenticationType: "Bearer"));

        [Fact]
        public async Task GetCurrentUserIdAsync_AnonymousRequest_RefusesAndProvisionsNobody()
        {
            var users = new FakeUserRepo();
            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => ServiceFor(anonymous, users).GetCurrentUserIdAsync());

            // The point of the guard: no oid means no row. This used to insert a User with a
            // null ExternalId into a NOT NULL uniquely-indexed column.
            Assert.Empty(users.Saved);
        }

        [Fact]
        public async Task GetCurrentUserIdAsync_NoHttpContext_Refuses()
        {
            var users = new FakeUserRepo();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => ServiceFor(principal: null, users).GetCurrentUserIdAsync());

            Assert.Empty(users.Saved);
        }

        [Fact]
        public async Task GetCurrentUserIdAsync_AccountAlreadyKnown_ReturnsItWithoutCreatingAnother()
        {
            var users = new FakeUserRepo(new User { Id = 5, ExternalId = "oid-alice", UserEmail = "alice@example.com" });

            var id = await ServiceFor(WorkforceToken("oid-alice", "alice@example.com"), users).GetCurrentUserIdAsync();

            Assert.Equal(5, id);
            Assert.Single(users.Saved);
        }

        [Fact]
        public async Task GetCurrentUserIdAsync_FirstSightOfWorkforceAccount_StoresPreferredUsername()
        {
            var users = new FakeUserRepo();

            await ServiceFor(WorkforceToken("oid-alice", "alice@example.com"), users).GetCurrentUserIdAsync();

            Assert.Equal("alice@example.com", users.Saved.Single().UserEmail);
        }

        [Fact]
        public async Task GetCurrentUserIdAsync_FirstSightOfCiamLocalAccount_FallsBackToTheEmailsClaim()
        {
            var users = new FakeUserRepo();

            await ServiceFor(CiamLocalToken("oid-bob", "bob@example.com"), users).GetCurrentUserIdAsync();

            // Without the fallback this was string.Empty - the address is only in `emails` on the
            // local email+password accounts this app actually signs up.
            Assert.Equal("bob@example.com", users.Saved.Single().UserEmail);
        }

        [Fact]
        public async Task GetCurrentUserIdAsync_TwoCiamLocalAccounts_BothGetIn()
        {
            var users = new FakeUserRepo();

            var firstId = await ServiceFor(CiamLocalToken("oid-bob", "bob@example.com"), users).GetCurrentUserIdAsync();
            var secondId = await ServiceFor(CiamLocalToken("oid-carol", "carol@example.com"), users).GetCurrentUserIdAsync();

            // The bug this replaces: both accounts stored an empty UserEmail, so the second one
            // collided on IX_Users_UserEmail and could never sign up at all.
            Assert.NotEqual(firstId, secondId);
            Assert.Equal(
                new[] { "bob@example.com", "carol@example.com" },
                users.Saved.Select(x => x.UserEmail));
        }
    }
}
