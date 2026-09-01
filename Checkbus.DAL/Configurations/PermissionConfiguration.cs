using Checkbus.BEL.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Checkbus.DAL.Configurations;

/// <summary>
/// Permissions are a global catalog (not tenant-scoped), seeded from the
/// fixed <see cref="PermissionKeys.All"/> list in order (fixed Ids 1..N).
/// </summary>
public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Key).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Description).HasMaxLength(300);

        builder.HasIndex(p => p.Key).IsUnique();

        builder.HasData(PermissionKeys.All
            .Select((key, index) => new { Id = index + 1, Key = key, Description = (string?)null })
            .ToArray());
    }
}
