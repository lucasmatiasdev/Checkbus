using Checkbus.Domain.Entities.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Checkbus.Infrastructure.Context.Configurations
{
    public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
    {
        public void Configure(EntityTypeBuilder<Organization> builder)
        {
            builder.HasKey(o => o.Id);

            builder.Property(o => o.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(o => o.CUIT)
                .IsRequired()
                .HasMaxLength(20);

            builder.HasIndex(o => o.CUIT)
                .IsUnique();

            builder.Property(o => o.IsActive)
                .IsRequired();
        }
    }
}
