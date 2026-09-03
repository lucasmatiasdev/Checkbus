using Checkbus.BEL.Fleet;
using Checkbus.DAL.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Checkbus.DAL.Configurations;

/// <summary>
/// Vehicles are strictly tenant-scoped. Plate is unique per organization.
/// </summary>
public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    private readonly CheckbusDbContext _context;

    public VehicleConfiguration(CheckbusDbContext context)
    {
        _context = context;
    }

    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Plate).IsRequired().HasMaxLength(20);
        builder.Property(v => v.CurrentDiagnosticState).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(v => new { v.OrganizationId, v.Plate }).IsUnique();

        builder.HasQueryFilter(v =>
            _context.IsSystemMode || v.OrganizationId == _context.CurrentOrganizationId);
    }
}
