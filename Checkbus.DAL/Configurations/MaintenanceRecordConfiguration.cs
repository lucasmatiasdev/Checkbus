using Checkbus.BEL.Fleet;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Checkbus.DAL.Configurations;

/// <summary>
/// No own tenant filter — reached only through <see cref="Vehicle"/>, which is
/// already tenant-filtered.
/// </summary>
public class MaintenanceRecordConfiguration : IEntityTypeConfiguration<MaintenanceRecord>
{
    public void Configure(EntityTypeBuilder<MaintenanceRecord> builder)
    {
        builder.ToTable("MaintenanceRecords");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.EventDate).IsRequired().HasColumnType("timestamptz");
        builder.Property(m => m.Description).IsRequired().HasMaxLength(1000);
        builder.Property(m => m.AffectedComponents).IsRequired().HasMaxLength(500);
        builder.Property(m => m.Status).IsRequired().HasConversion<string>().HasMaxLength(20);

        builder.HasOne(m => m.Vehicle)
            .WithMany(v => v.MaintenanceRecords)
            .HasForeignKey(m => m.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
