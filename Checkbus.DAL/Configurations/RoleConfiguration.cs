using Checkbus.BEL.Auth;
using Checkbus.DAL.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Checkbus.DAL.Configurations;

/// <summary>
/// Predefined (global, <c>OrganizationId == null</c>) roles are seeded here
/// with fixed Ids 1-5 and shared by every tenant; custom roles are scoped to
/// a single organization. The query filter is disjunctive on purpose: in SQL,
/// <c>NULL</c> never equals a tenant id, so a plain equality filter
/// (<c>OrganizationId == tenantId</c>) would silently hide every global row.
/// </summary>
public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    private readonly CheckbusDbContext _context;

    public RoleConfiguration(CheckbusDbContext context)
    {
        _context = context;
    }

    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).IsRequired().HasMaxLength(100);

        // Unique role name among the global/predefined roles.
        builder.HasIndex(r => r.Name)
            .IsUnique()
            .HasFilter("\"OrganizationId\" IS NULL")
            .HasDatabaseName("IX_Roles_Name_Global");

        // Unique role name per organization among custom roles. Two different
        // tenants may each freely have their own "Chofer"-named custom role.
        builder.HasIndex(r => new { r.OrganizationId, r.Name })
            .IsUnique()
            .HasFilter("\"OrganizationId\" IS NOT NULL")
            .HasDatabaseName("IX_Roles_OrganizationId_Name_Custom");

        builder.HasQueryFilter(r =>
            _context.IsSystemMode
            || r.OrganizationId == null
            || r.OrganizationId == _context.CurrentOrganizationId);

        builder.HasData(
            new { Id = RoleNames.PlanificadorId, Name = RoleNames.Planificador, OrganizationId = (int?)null },
            new { Id = RoleNames.MantenimientoId, Name = RoleNames.Mantenimiento, OrganizationId = (int?)null },
            new { Id = RoleNames.ValidadorId, Name = RoleNames.Validador, OrganizationId = (int?)null },
            new { Id = RoleNames.ChoferId, Name = RoleNames.Chofer, OrganizationId = (int?)null },
            new { Id = RoleNames.AdministradorId, Name = RoleNames.Administrador, OrganizationId = (int?)null });
    }
}
