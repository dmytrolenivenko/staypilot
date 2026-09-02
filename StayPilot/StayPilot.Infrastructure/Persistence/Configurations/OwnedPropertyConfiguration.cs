using Microsoft.EntityFrameworkCore;
using StayPilot.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StayPilot.Infrastructure.Persistence.Configurations
{
    public class OwnedPropertyConfiguration : IEntityTypeConfiguration<OwnedProperty>
    {
        public void Configure(EntityTypeBuilder<OwnedProperty> builder)
        {
            builder.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
