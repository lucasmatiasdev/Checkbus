using Checkbus.BEL.Auth;
using Checkbus.DAL.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Checkbus.DAL.Configurations;

/// <summary>
/// Grants a <see cref="Permission"/> to a <see cref="Role"/>. The query
/// filter is navigation-based (through the required <see cref="RolePermission.Role"/>
/// navigation) rather than duplicating a tenant column here, so a direct
/// query against <c>RolePermissions</c> — bypassing the <c>Role</c> navigation
/// in application code — still cannot see another tenant's custom-role
/// grants: EF applies this predicate to every query against this entity type.
/// </summary>
public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    private readonly CheckbusDbContext _context;

    public RolePermissionConfiguration(CheckbusDbContext context)
    {
        _context = context;
    }

    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");

        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

        builder.HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(rp =>
            _context.IsSystemMode
            || rp.Role.OrganizationId == null
            || rp.Role.OrganizationId == _context.CurrentOrganizationId);

        builder.HasData(
            // Administrador: full access.
            Grant(RoleNames.AdministradorId, PermissionKeys.UsersManage),
            Grant(RoleNames.AdministradorId, PermissionKeys.RolesManage),
            Grant(RoleNames.AdministradorId, PermissionKeys.ReportsView),
            Grant(RoleNames.AdministradorId, PermissionKeys.ReportsManage),
            Grant(RoleNames.AdministradorId, PermissionKeys.RoutesManage),
            Grant(RoleNames.AdministradorId, PermissionKeys.TripsManage),
            Grant(RoleNames.AdministradorId, PermissionKeys.VehiclesManage),
            Grant(RoleNames.AdministradorId, PermissionKeys.IncidentsManage),
            Grant(RoleNames.AdministradorId, PermissionKeys.IncidentsValidate),

            // Planificador: planning + reporting. Shares reports.view with
            // Administrador — the transversal-permission requirement.
            Grant(RoleNames.PlanificadorId, PermissionKeys.RoutesManage),
            Grant(RoleNames.PlanificadorId, PermissionKeys.TripsManage),
            Grant(RoleNames.PlanificadorId, PermissionKeys.ReportsView),

            // Mantenimiento: fleet + incidents.
            Grant(RoleNames.MantenimientoId, PermissionKeys.VehiclesManage),
            Grant(RoleNames.MantenimientoId, PermissionKeys.IncidentsManage),

            // Validador: incident validation + reporting.
            Grant(RoleNames.ValidadorId, PermissionKeys.IncidentsValidate),
            Grant(RoleNames.ValidadorId, PermissionKeys.ReportsView),

            // Chofer: operates trips.
            Grant(RoleNames.ChoferId, PermissionKeys.TripsDrive));
    }

    private static object Grant(int roleId, string permissionKey)
    {
        var permissionId = Array.IndexOf(PermissionKeys.All.ToArray(), permissionKey) + 1;
        return new { RoleId = roleId, PermissionId = permissionId };
    }
}
