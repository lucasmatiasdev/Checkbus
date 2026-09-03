using Checkbus.BEL.Fleet;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Checkbus.DAL.Configurations;

/// <summary>
/// No own tenant filter — reached only through <see cref="Vehicle"/>, which is
/// already tenant-filtered. Document number is unique per vehicle.
/// </summary>
public class VehicleDocumentationConfiguration : IEntityTypeConfiguration<VehicleDocumentation>
{
    public void Configure(EntityTypeBuilder<VehicleDocumentation> builder)
    {
        builder.ToTable("VehicleDocumentations");

        builder.HasKey(vd => vd.Id);

        builder.Property(vd => vd.Type).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(vd => vd.DocumentNumber).IsRequired().HasMaxLength(50);
        builder.Property(vd => vd.State).IsRequired().HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(vd => new { vd.VehicleId, vd.DocumentNumber }).IsUnique();

        builder.HasOne(vd => vd.Vehicle)
            .WithMany(v => v.Documentations)
            .HasForeignKey(vd => vd.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
