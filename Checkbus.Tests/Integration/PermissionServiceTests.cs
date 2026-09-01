using Checkbus.BEL.Auth;
using Checkbus.BLL.Auth;
using Organization = Checkbus.BEL.Organization.Organization;

namespace Checkbus.Tests.Integration;

/// <summary>
/// S2.11 — <see cref="IPermissionService.HasPermissionAsync"/> must resolve a
/// user's effective permissions through <c>User -&gt; Role -&gt; RolePermission
/// -&gt; Permission</c>, correctly reflecting that <c>reports.view</c> is a
/// transversal permission shared by both Administrador and Planificador.
/// </summary>
public class PermissionServiceTests
{
    [Fact]
    public async Task Administrador_And_Planificador_Both_Have_Reports_View()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();
        int orgId;
        int adminUserId;
        int planificadorUserId;

        using (var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true }))
        {
            systemContext.Database.EnsureCreated();

            var org = new Organization { Name = "Org A", Cuit = "30-14141414-1" };
            systemContext.Organizations.Add(org);
            systemContext.SaveChanges();
            orgId = org.Id;

            var adminUser = new User
            {
                OrganizationId = orgId,
                Email = "admin@org-a.test",
                FullName = "Admin User",
                PasswordHash = "hash",
                RoleId = RoleNames.AdministradorId,
            };
            var planificadorUser = new User
            {
                OrganizationId = orgId,
                Email = "planificador@org-a.test",
                FullName = "Planificador User",
                PasswordHash = "hash",
                RoleId = RoleNames.PlanificadorId,
            };
            systemContext.Users.AddRange(adminUser, planificadorUser);
            systemContext.SaveChanges();
            adminUserId = adminUser.Id;
            planificadorUserId = planificadorUser.Id;
        }

        var factory = new FakeDbContextFactory(connection, new FakeTenantProvider { CurrentOrganizationId = orgId });
        IPermissionService sut = new PermissionService(factory);

        Assert.True(await sut.HasPermissionAsync(adminUserId, PermissionKeys.ReportsView));
        Assert.True(await sut.HasPermissionAsync(planificadorUserId, PermissionKeys.ReportsView));
    }

    [Fact]
    public async Task User_Without_The_Granted_Permission_Key_Returns_False()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();
        int orgId;
        int choferUserId;

        using (var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true }))
        {
            systemContext.Database.EnsureCreated();

            var org = new Organization { Name = "Org A", Cuit = "30-15151515-1" };
            systemContext.Organizations.Add(org);
            systemContext.SaveChanges();
            orgId = org.Id;

            var choferUser = new User
            {
                OrganizationId = orgId,
                Email = "chofer@org-a.test",
                FullName = "Chofer User",
                PasswordHash = "hash",
                RoleId = RoleNames.ChoferId,
            };
            systemContext.Users.Add(choferUser);
            systemContext.SaveChanges();
            choferUserId = choferUser.Id;
        }

        var factory = new FakeDbContextFactory(connection, new FakeTenantProvider { CurrentOrganizationId = orgId });
        IPermissionService sut = new PermissionService(factory);

        Assert.False(await sut.HasPermissionAsync(choferUserId, PermissionKeys.ReportsView));
    }
}
