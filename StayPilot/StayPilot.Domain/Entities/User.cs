
namespace StayPilot.Domain.Entities
{
    /// <summary>
    /// A person who signs in to StayPilot and owns properties.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Database Id for this user.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Id from Entra (the oid/sub claim). Used to find the user on login.
        /// </summary>
        public string ExternalId { get; set; } = string.Empty;

        /// <summary>
        /// Display name for this user.
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Email for this user. Must be unique.
        /// </summary>
        public string UserEmail { get; set; } = string.Empty;

        /// <summary>
        /// Preferred UI language. Can be empty if not set yet.
        /// </summary>
        public string? PreferredLocale { get; set; }

        /// <summary>
        /// When this user was created (UTC time). Defaults to now.
        /// </summary>
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
