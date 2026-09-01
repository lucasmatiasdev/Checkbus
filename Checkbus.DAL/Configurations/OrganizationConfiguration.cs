using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Organization = Checkbus.BEL.Organization.Organization;

namespace Checkbus.DAL.Configurations;

/// <summary>
/// Organization is the tenant root itself, so it carries no
/// <c>OrganizationId</c> and needs no tenant query filter.
/// </summary>
public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organizations");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name).IsRequired().HasMaxLength(200);
        builder.Property(o => o.Cuit).IsRequired().HasMaxLength(20);

        builder.HasIndex(o => o.Cuit).IsUnique();
    }
}
