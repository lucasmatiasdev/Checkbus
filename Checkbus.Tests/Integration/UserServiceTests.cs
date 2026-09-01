using Checkbus.BEL.Auth;
using Checkbus.BLL.Auth;
using Organization = Checkbus.BEL.Organization.Organization;

namespace Checkbus.Tests.Integration;

/// <summary>
/// S2.15/S2.16 — <see cref="IUserService"/> enforces the role-assignment
/// scoping invariant (Decision #28): a <see cref="User"/> can only be
/// assigned a <see cref="Role"/> that is global (<c>OrganizationId == null</c>)
/// or belongs to that user's own organization.
/// </summary>
public class UserServiceTests
{
    [Fact]
    public async Task AssignRoleAsync_Throws_When_Role_Belongs_To_A_Different_Organization()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();
        int orgAId;
        int userId;
        int customRoleBId;

        using (var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true }))
        {
            systemContext.Database.EnsureCreated();

            var orgA = new Organization { Name = "Org A", Cuit = "30-16161616-1" };
            var orgB = new Organization { Name = "Org B", Cuit = "30-17171717-1" };
            systemContext.Organizations.AddRange(orgA, orgB);
            systemContext.SaveChanges();
            orgAId = orgA.Id;
            var orgBId = orgB.Id;

            var user = new User
            {
                OrganizationId = orgAId,
                Email = "user@org-a.test",
                FullName = "User A",
                PasswordHash = "hash",
                RoleId = RoleNames.ChoferId,
            };
            systemContext.Users.Add(user);
            systemContext.SaveChanges();
            userId = user.Id;

            var customRoleB = new Role { Name = "Custom B", OrganizationId = orgBId };
            systemContext.Roles.Add(customRoleB);
            systemContext.SaveChanges();
            customRoleBId = customRoleB.Id;
        }

        var factory = new FakeDbContextFactory(connection, new FakeTenantProvider { CurrentOrganizationId = orgAId });
        IUserService sut = new UserService(factory);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.AssignRoleAsync(userId, customRoleBId));
    }

    [Fact]
    public async Task AssignRoleAsync_Succeeds_For_A_Global_Predefined_Role()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();
        int orgAId;
        int userId;

        using (var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true }))
        {
            systemContext.Database.EnsureCreated();

            var orgA = new Organization { Name = "Org A", Cuit = "30-18181818-1" };
            systemContext.Organizations.Add(orgA);
            systemContext.SaveChanges();
            orgAId = orgA.Id;

            var user = new User
            {
                OrganizationId = orgAId,
                Email = "user2@org-a.test",
                FullName = "User A2",
                PasswordHash = "hash",
                RoleId = RoleNames.ChoferId,
            };
            systemContext.Users.Add(user);
            systemContext.SaveChanges();
            userId = user.Id;
        }

        var factory = new FakeDbContextFactory(connection, new FakeTenantProvider { CurrentOrganizationId = orgAId });
        IUserService sut = new UserService(factory);

        await sut.AssignRoleAsync(userId, RoleNames.AdministradorId);

        using var verifyContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { CurrentOrganizationId = orgAId });
        var updatedUser = verifyContext.Users.Single(u => u.Id == userId);
        Assert.Equal(RoleNames.AdministradorId, updatedUser.RoleId);
    }

    [Fact]
    public async Task AssignRoleAsync_Succeeds_For_A_Custom_Role_In_The_Same_Organization()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();
        int orgAId;
        int userId;
        int customRoleAId;

        using (var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true }))
        {
            systemContext.Database.EnsureCreated();

            var orgA = new Organization { Name = "Org A", Cuit = "30-19191919-1" };
            systemContext.Organizations.Add(orgA);
            systemContext.SaveChanges();
            orgAId = orgA.Id;

            var user = new User
            {
                OrganizationId = orgAId,
                Email = "user3@org-a.test",
                FullName = "User A3",
                PasswordHash = "hash",
                RoleId = RoleNames.ChoferId,
            };
            systemContext.Users.Add(user);
            systemContext.SaveChanges();
            userId = user.Id;

            var customRoleA = new Role { Name = "Custom A", OrganizationId = orgAId };
            systemContext.Roles.Add(customRoleA);
            systemContext.SaveChanges();
            customRoleAId = customRoleA.Id;
        }

        var factory = new FakeDbContextFactory(connection, new FakeTenantProvider { CurrentOrganizationId = orgAId });
        IUserService sut = new UserService(factory);

        await sut.AssignRoleAsync(userId, customRoleAId);

        using var verifyContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { CurrentOrganizationId = orgAId });
        var updatedUser = verifyContext.Users.Single(u => u.Id == userId);
        Assert.Equal(customRoleAId, updatedUser.RoleId);
    }
}
