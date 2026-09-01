using Checkbus.BEL.Auth;
using Organization = Checkbus.BEL.Organization.Organization;

namespace Checkbus.Tests.Integration;

/// <summary>
/// S2.9 — custom-role isolation across tenants, shared predefined-role
/// identity across tenants, RolePermission tenant isolation even when queried
/// directly (bypassing the Role navigation), and predefined-role
/// immutability outside system mode.
/// </summary>
public class RbacMultiTenantTests
{
    [Fact]
    public void Two_Tenants_Can_Each_Have_Their_Own_Chofer_Custom_Role_Without_Collision()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();

        int orgAId;
        int orgBId;

        using (var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true }))
        {
            systemContext.Database.EnsureCreated();

            var orgA = new Organization { Name = "Org A", Cuit = "30-55555555-5" };
            var orgB = new Organization { Name = "Org B", Cuit = "30-66666666-6" };
            systemContext.Organizations.AddRange(orgA, orgB);
            systemContext.SaveChanges();
            orgAId = orgA.Id;
            orgBId = orgB.Id;

            systemContext.Roles.Add(new Role { Name = "Chofer", OrganizationId = orgAId });
            systemContext.Roles.Add(new Role { Name = "Chofer", OrganizationId = orgBId });

            // Must not throw: the unique index is scoped per-organization, so
            // two tenants each having a custom "Chofer" role does not collide.
            systemContext.SaveChanges();
        }

        using var verifyContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true });

        var customChoferRoles = verifyContext.Roles
            .Where(r => r.Name == "Chofer" && r.OrganizationId != null)
            .ToList();

        Assert.Equal(2, customChoferRoles.Count);
    }

    [Fact]
    public void Predefined_Roles_Resolve_To_Same_Shared_Ids_For_Every_Tenant()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();
        int orgAId;
        int orgBId;

        using (var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true }))
        {
            systemContext.Database.EnsureCreated();

            var orgA = new Organization { Name = "Org A", Cuit = "30-77777777-7" };
            var orgB = new Organization { Name = "Org B", Cuit = "30-88888888-8" };
            systemContext.Organizations.AddRange(orgA, orgB);
            systemContext.SaveChanges();
            orgAId = orgA.Id;
            orgBId = orgB.Id;
        }

        using var tenantAContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { CurrentOrganizationId = orgAId });
        using var tenantBContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { CurrentOrganizationId = orgBId });

        var predefinedForA = tenantAContext.Roles
            .Where(r => r.OrganizationId == null)
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, r.Name })
            .ToList();
        var predefinedForB = tenantBContext.Roles
            .Where(r => r.OrganizationId == null)
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, r.Name })
            .ToList();

        Assert.Equal(5, predefinedForA.Count);
        Assert.Equal(predefinedForA, predefinedForB);
        Assert.Contains(predefinedForA, r => r.Id == RoleNames.AdministradorId && r.Name == RoleNames.Administrador);
    }

    [Fact]
    public void Custom_Role_Of_Org_A_Is_Not_Visible_When_Querying_As_Org_B()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();
        int orgAId;
        int orgBId;

        using (var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true }))
        {
            systemContext.Database.EnsureCreated();

            var orgA = new Organization { Name = "Org A", Cuit = "30-99999999-9" };
            var orgB = new Organization { Name = "Org B", Cuit = "30-10101010-1" };
            systemContext.Organizations.AddRange(orgA, orgB);
            systemContext.SaveChanges();
            orgAId = orgA.Id;
            orgBId = orgB.Id;

            systemContext.Roles.Add(new Role { Name = "Supervisor Regional", OrganizationId = orgAId });
            systemContext.SaveChanges();
        }

        using var tenantBContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { CurrentOrganizationId = orgBId });

        var visibleToB = tenantBContext.Roles.Where(r => r.Name == "Supervisor Regional").ToList();

        Assert.Empty(visibleToB);
    }

    [Fact]
    public void Direct_RolePermissions_Query_Does_Not_Leak_Another_Tenants_Custom_Role_Grants()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();
        int orgAId;
        int orgBId;
        int customRoleId;
        int permissionId;

        using (var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true }))
        {
            systemContext.Database.EnsureCreated();

            var orgA = new Organization { Name = "Org A", Cuit = "30-12121212-1" };
            var orgB = new Organization { Name = "Org B", Cuit = "30-13131313-1" };
            systemContext.Organizations.AddRange(orgA, orgB);
            systemContext.SaveChanges();
            orgAId = orgA.Id;
            orgBId = orgB.Id;

            var customRole = new Role { Name = "Auditor Interno", OrganizationId = orgAId };
            systemContext.Roles.Add(customRole);
            systemContext.SaveChanges();
            customRoleId = customRole.Id;

            permissionId = systemContext.Permissions
                .Single(p => p.Key == PermissionKeys.ReportsView).Id;

            systemContext.RolePermissions.Add(new RolePermission
            {
                RoleId = customRoleId,
                PermissionId = permissionId,
            });
            systemContext.SaveChanges();
        }

        using var tenantBContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { CurrentOrganizationId = orgBId });

        // Bypasses the Role navigation on purpose: a raw query against the
        // RolePermissions DbSet must still be filtered by tenant.
        var leakedGrants = tenantBContext.RolePermissions
            .Where(rp => rp.RoleId == customRoleId)
            .ToList();

        Assert.Empty(leakedGrants);
    }

    [Fact]
    public void Updating_A_Predefined_Role_Outside_System_Mode_Throws()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();

        using (var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true }))
        {
            systemContext.Database.EnsureCreated();
        }

        using var tenantContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { CurrentOrganizationId = 1 });

        var administrador = tenantContext.Roles.Single(r => r.Id == RoleNames.AdministradorId);
        administrador.Name = "Administrador Renombrado";

        Assert.Throws<InvalidOperationException>(() => tenantContext.SaveChanges());
    }

    [Fact]
    public void Deleting_A_Predefined_Role_Outside_System_Mode_Throws()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();

        using (var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true }))
        {
            systemContext.Database.EnsureCreated();
        }

        using var tenantContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { CurrentOrganizationId = 1 });

        var chofer = tenantContext.Roles.Single(r => r.Id == RoleNames.ChoferId);
        tenantContext.Roles.Remove(chofer);

        Assert.Throws<InvalidOperationException>(() => tenantContext.SaveChanges());
    }
}
