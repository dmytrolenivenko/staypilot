using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StayPilot.Domain.Entities;

namespace StayPilot.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Maps the User entity to its table and sets the unique lookup columns.
    /// </summary>
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        /// <summary>
        /// ExternalId and UserEmail must both be unique across all users.
        /// </summary>
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasIndex(x => x.ExternalId).IsUnique();
            builder.HasIndex(x => x.UserEmail).IsUnique();
        }
    }
}
