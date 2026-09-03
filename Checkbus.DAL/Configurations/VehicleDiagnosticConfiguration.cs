using Checkbus.BEL.Fleet;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Checkbus.DAL.Configurations;

/// <summary>
/// No own tenant filter — reached only through <see cref="Vehicle"/>, which is
/// already tenant-filtered.
/// </summary>
public class VehicleDiagnosticConfiguration : IEntityTypeConfiguration<VehicleDiagnostic>
{
    public void Configure(EntityTypeBuilder<VehicleDiagnostic> builder)
    {
        builder.ToTable("VehicleDiagnostics");

        builder.HasKey(vd => vd.Id);

        builder.Property(vd => vd.OverallState).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(vd => vd.RecordedAt).IsRequired().HasColumnType("timestamptz");

        builder.HasOne(vd => vd.Vehicle)
            .WithMany(v => v.Diagnostics)
            .HasForeignKey(vd => vd.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
