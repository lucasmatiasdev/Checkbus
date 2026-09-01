using Checkbus.BEL.Auth;
using Checkbus.DAL.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Checkbus.DAL.Configurations;

/// <summary>
/// Users are strictly tenant-scoped: exactly one organization, enforced both
/// by the non-nullable <see cref="User.OrganizationId"/> and by the standard
/// (non-disjunctive) tenant query filter below. Login is by
/// <see cref="User.Email"/> alone, with no tenant selector (CU-01), so the
/// email uniqueness index is global rather than per-organization.
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    private readonly CheckbusDbContext _context;

    public UserConfiguration(CheckbusDbContext context)
    {
        _context = context;
    }

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.Property(u => u.FullName).IsRequired().HasMaxLength(200);
        builder.Property(u => u.PasswordHash).IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();

        builder.HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(u =>
            _context.IsSystemMode || u.OrganizationId == _context.CurrentOrganizationId);
    }
}
