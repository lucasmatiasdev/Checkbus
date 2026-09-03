using Checkbus.BEL.Fleet;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Checkbus.DAL.Configurations;

/// <summary>
/// 1—1 with <see cref="Driver"/>. No own tenant filter — a <see cref="License"/>
/// is only ever reachable through its owning <see cref="Driver"/>, which is
/// already tenant-filtered.
/// </summary>
public class LicenseConfiguration : IEntityTypeConfiguration<License>
{
    public void Configure(EntityTypeBuilder<License> builder)
    {
        builder.ToTable("Licenses");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Category).IsRequired().HasConversion<string>().HasMaxLength(10);
        builder.Property(l => l.ExpiryDate).IsRequired().HasColumnType("timestamptz");

        builder.HasIndex(l => l.DriverId).IsUnique();

        builder.HasOne(l => l.Driver)
            .WithOne(d => d.License)
            .HasForeignKey<License>(l => l.DriverId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
