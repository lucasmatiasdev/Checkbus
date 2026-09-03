using Checkbus.BEL.Fleet;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Checkbus.DAL.Configurations;

/// <summary>
/// No own tenant filter — reached only through
/// <c>VehicleDiagnostic.Vehicle</c>, which is already tenant-filtered.
/// </summary>
public class ComponentDiagnosticConfiguration : IEntityTypeConfiguration<ComponentDiagnostic>
{
    public void Configure(EntityTypeBuilder<ComponentDiagnostic> builder)
    {
        builder.ToTable("ComponentDiagnostics");

        builder.HasKey(cd => cd.Id);

        builder.Property(cd => cd.Component).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(cd => cd.Rating).IsRequired().HasConversion<string>().HasMaxLength(20);

        builder.HasOne(cd => cd.VehicleDiagnostic)
            .WithMany(vd => vd.Components)
            .HasForeignKey(cd => cd.VehicleDiagnosticId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
