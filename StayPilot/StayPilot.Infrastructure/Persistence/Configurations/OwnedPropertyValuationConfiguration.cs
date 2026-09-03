using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StayPilot.Domain.Entities;

namespace StayPilot.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Maps the OwnedPropertyValuation entity to its table: one row per OwnedProperty, sharing
    /// its primary key instead of carrying an identity of its own.
    /// </summary>
    public class OwnedPropertyValuationConfiguration : IEntityTypeConfiguration<OwnedPropertyValuation>
    {
        public void Configure(EntityTypeBuilder<OwnedPropertyValuation> builder)
        {
            builder.HasKey(x => x.OwnedPropertyId);

            // Deleting the property takes its cached valuation with it - there is nothing for a
            // valuation row to mean once the property it prices is gone.
            builder.HasOne(x => x.OwnedProperty)
                .WithOne()
                .HasForeignKey<OwnedPropertyValuation>(x => x.OwnedPropertyId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
