using Checkbus.BEL.Fleet;
using Checkbus.DAL.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Checkbus.DAL.Configurations;

/// <summary>
/// Drivers are strictly tenant-scoped: exactly one organization, enforced
/// both by the non-nullable <see cref="Driver.OrganizationId"/> and by the
/// standard tenant query filter. DNI is unique per organization.
/// </summary>
public class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    private readonly CheckbusDbContext _context;

    public DriverConfiguration(CheckbusDbContext context)
    {
        _context = context;
    }

    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.ToTable("Drivers");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.FullName).IsRequired().HasMaxLength(200);
        builder.Property(d => d.Dni).IsRequired().HasMaxLength(20);

        builder.HasIndex(d => new { d.OrganizationId, d.Dni }).IsUnique();

        builder.HasQueryFilter(d =>
            _context.IsSystemMode || d.OrganizationId == _context.CurrentOrganizationId);
    }
}
