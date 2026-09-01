using Checkbus.BEL.Auth;
using Checkbus.BLL.Auth;
using Microsoft.AspNetCore.Identity;
using Organization = Checkbus.BEL.Organization.Organization;

namespace Checkbus.Tests.Integration;

/// <summary>
/// S2.17 — <see cref="IAuthService"/> implements CU-01: login by e-mail
/// alone (no tenant selector), returning the user's own
/// <c>OrganizationId</c>/<c>RoleId</c> so the caller can build the sign-in
/// claims without a second round trip.
/// </summary>
public class AuthServiceTests
{
    private static readonly IPasswordHasher<User> Hasher = new PasswordHasher<User>();

    [Fact]
    public async Task AuthenticateAsync_Returns_The_User_With_Its_Own_Organization_And_Role_On_A_Correct_Password()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();
        int orgAId;

        using (var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true }))
        {
            systemContext.Database.EnsureCreated();

            var orgA = new Organization { Name = "Org A", Cuit = "30-25252525-1" };
            systemContext.Organizations.Add(orgA);
            systemContext.SaveChanges();
            orgAId = orgA.Id;

            var user = new User
            {
                OrganizationId = orgAId,
                Email = "admin@org-a.test",
                FullName = "Admin Org A",
                PasswordHash = string.Empty,
                RoleId = RoleNames.AdministradorId,
            };
            user.PasswordHash = Hasher.HashPassword(user, "Sup3rSecret!");
            systemContext.Users.Add(user);
            systemContext.SaveChanges();
        }

        var factory = new FakeDbContextFactory(connection, new FakeTenantProvider());
        IAuthService sut = new AuthService(factory, Hasher);

        var result = await sut.AuthenticateAsync("admin@org-a.test", "Sup3rSecret!");

        Assert.NotNull(result);
        Assert.Equal(orgAId, result!.OrganizationId);
        Assert.Equal(RoleNames.AdministradorId, result.RoleId);
        Assert.Equal(RoleNames.Administrador, result.RoleName);
        Assert.Equal("Admin Org A", result.FullName);
    }

    [Fact]
    public async Task AuthenticateAsync_Returns_Null_For_A_Wrong_Password()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();

        using (var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true }))
        {
            systemContext.Database.EnsureCreated();

            var orgA = new Organization { Name = "Org A", Cuit = "30-26262626-1" };
            systemContext.Organizations.Add(orgA);
            systemContext.SaveChanges();

            var user = new User
            {
                OrganizationId = orgA.Id,
                Email = "user@org-a.test",
                FullName = "User Org A",
                PasswordHash = string.Empty,
                RoleId = RoleNames.ChoferId,
            };
            user.PasswordHash = Hasher.HashPassword(user, "CorrectPassword!");
            systemContext.Users.Add(user);
            systemContext.SaveChanges();
        }

        var factory = new FakeDbContextFactory(connection, new FakeTenantProvider());
        IAuthService sut = new AuthService(factory, Hasher);

        var result = await sut.AuthenticateAsync("user@org-a.test", "WrongPassword!");

        Assert.Null(result);
    }

    [Fact]
    public async Task AuthenticateAsync_Returns_Null_For_An_Unknown_Email()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();

        using (var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true }))
        {
            systemContext.Database.EnsureCreated();
        }

        var factory = new FakeDbContextFactory(connection, new FakeTenantProvider());
        IAuthService sut = new AuthService(factory, Hasher);

        var result = await sut.AuthenticateAsync("nobody@nowhere.test", "AnyPassword!");

        Assert.Null(result);
    }

    [Fact]
    public async Task AuthenticateAsync_Returns_Null_For_An_Inactive_User_Even_With_The_Correct_Password()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();

        using (var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true }))
        {
            systemContext.Database.EnsureCreated();

            var orgA = new Organization { Name = "Org A", Cuit = "30-27272727-1" };
            systemContext.Organizations.Add(orgA);
            systemContext.SaveChanges();

            var user = new User
            {
                OrganizationId = orgA.Id,
                Email = "inactive@org-a.test",
                FullName = "Inactive User",
                PasswordHash = string.Empty,
                RoleId = RoleNames.ChoferId,
                IsActive = false,
            };
            user.PasswordHash = Hasher.HashPassword(user, "Sup3rSecret!");
            systemContext.Users.Add(user);
            systemContext.SaveChanges();
        }

        var factory = new FakeDbContextFactory(connection, new FakeTenantProvider());
        IAuthService sut = new AuthService(factory, Hasher);

        var result = await sut.AuthenticateAsync("inactive@org-a.test", "Sup3rSecret!");

        Assert.Null(result);
    }
}
