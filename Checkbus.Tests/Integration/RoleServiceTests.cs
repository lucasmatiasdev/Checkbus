using Checkbus.BEL.Auth;
using Checkbus.BLL.Auth;
using Organization = Checkbus.BEL.Organization.Organization;

namespace Checkbus.Tests.Integration;

/// <summary>
/// S2.16 — <see cref="IRoleService"/> manages custom roles scoped to a single
/// organization and gives a clear, service-level rejection when a caller
/// attempts to update or delete a predefined (global) role, rather than only
/// relying on the DbContext-level <c>SaveChanges</c> guard from PR1.
/// </summary>
public class RoleServiceTests
{
    [Fact]
    public async Task CreateCustomRoleAsync_Creates_A_Role_Scoped_To_The_Organization()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();
        int orgAId;

        using (var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true }))
        {
            systemContext.Database.EnsureCreated();

            var orgA = new Organization { Name = "Org A", Cuit = "30-21212121-1" };
            systemContext.Organizations.Add(orgA);
            systemContext.SaveChanges();
            orgAId = orgA.Id;
        }

        var factory = new FakeDbContextFactory(connection, new FakeTenantProvider { CurrentOrganizationId = orgAId });
        IRoleService sut = new RoleService(factory);

        var created = await sut.CreateCustomRoleAsync(orgAId, "Supervisor Regional");

        Assert.Equal(orgAId, created.OrganizationId);
        Assert.Equal("Supervisor Regional", created.Name);
    }

    [Fact]
    public async Task UpdateCustomRoleAsync_Throws_For_A_Predefined_Role()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();
        using (var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true }))
        {
            systemContext.Database.EnsureCreated();
        }

        var factory = new FakeDbContextFactory(connection, new FakeTenantProvider { CurrentOrganizationId = 1 });
        IRoleService sut = new RoleService(factory);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.UpdateCustomRoleAsync(RoleNames.AdministradorId, "Nombre Nuevo"));
    }

    [Fact]
    public async Task DeleteCustomRoleAsync_Throws_For_A_Predefined_Role()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();
        using (var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true }))
        {
            systemContext.Database.EnsureCreated();
        }

        var factory = new FakeDbContextFactory(connection, new FakeTenantProvider { CurrentOrganizationId = 1 });
        IRoleService sut = new RoleService(factory);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.DeleteCustomRoleAsync(RoleNames.ChoferId));
    }

    [Fact]
    public async Task UpdateCustomRoleAsync_Renames_An_Existing_Custom_Role()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();
        int orgAId;
        int customRoleId;

        using (var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true }))
        {
            systemContext.Database.EnsureCreated();

            var orgA = new Organization { Name = "Org A", Cuit = "30-22222222-3" };
            systemContext.Organizations.Add(orgA);
            systemContext.SaveChanges();
            orgAId = orgA.Id;

            var customRole = new Role { Name = "Antiguo", OrganizationId = orgAId };
            systemContext.Roles.Add(customRole);
            systemContext.SaveChanges();
            customRoleId = customRole.Id;
        }

        var factory = new FakeDbContextFactory(connection, new FakeTenantProvider { CurrentOrganizationId = orgAId });
        IRoleService sut = new RoleService(factory);

        await sut.UpdateCustomRoleAsync(customRoleId, "Nuevo Nombre");

        using var verifyContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { CurrentOrganizationId = orgAId });
        var updated = verifyContext.Roles.Single(r => r.Id == customRoleId);
        Assert.Equal("Nuevo Nombre", updated.Name);
    }
}
