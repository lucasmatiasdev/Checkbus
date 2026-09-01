using Checkbus.BEL.Auth;
using Checkbus.BLL.Organization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Organization = Checkbus.BEL.Organization.Organization;

namespace Checkbus.Tests.Integration;

/// <summary>
/// S2.17 — <see cref="ICompanyRegistrationService"/> implements CU-02: create
/// the <see cref="Organization"/> tenant and its initial <c>Administrador</c>
/// user atomically. A failure on the user insert (e.g. a duplicate e-mail)
/// must not leave a dangling organization behind.
/// </summary>
public class CompanyRegistrationServiceTests
{
    private static readonly IPasswordHasher<User> Hasher = new PasswordHasher<User>();

    [Fact]
    public async Task RegisterCompanyAsync_Creates_The_Organization_And_An_Administrador_User()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();

        using (var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true }))
        {
            systemContext.Database.EnsureCreated();
        }

        var factory = new FakeDbContextFactory(connection, new FakeTenantProvider { IsSystemMode = true });
        ICompanyRegistrationService sut = new CompanyRegistrationService(factory, Hasher);

        var result = await sut.RegisterCompanyAsync(
            "Empresa Nueva SA", "30-71717171-1", "Admin Inicial", "admin@empresa-nueva.test", "Sup3rSecret!");

        Assert.Equal(RoleNames.AdministradorId, result.AdminRoleId);
        Assert.Equal(RoleNames.Administrador, result.AdminRoleName);

        using var verifyContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true });

        var organization = verifyContext.Organizations.IgnoreQueryFilters()
            .Single(o => o.Id == result.OrganizationId);
        Assert.Equal("Empresa Nueva SA", organization.Name);
        Assert.Equal("30-71717171-1", organization.Cuit);

        var admin = verifyContext.Users.IgnoreQueryFilters().Single(u => u.Id == result.AdminUserId);
        Assert.Equal(result.OrganizationId, admin.OrganizationId);
        Assert.Equal(RoleNames.AdministradorId, admin.RoleId);
        Assert.Equal("admin@empresa-nueva.test", admin.Email);
        Assert.NotEqual("Sup3rSecret!", admin.PasswordHash);
    }

    [Fact]
    public async Task RegisterCompanyAsync_Throws_And_Leaves_No_Organization_When_The_Cuit_Already_Exists()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();

        using (var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true }))
        {
            systemContext.Database.EnsureCreated();

            systemContext.Organizations.Add(new Organization { Name = "Existente SA", Cuit = "30-72727272-2" });
            systemContext.SaveChanges();
        }

        var factory = new FakeDbContextFactory(connection, new FakeTenantProvider { IsSystemMode = true });
        ICompanyRegistrationService sut = new CompanyRegistrationService(factory, Hasher);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RegisterCompanyAsync(
            "Otra Empresa SA", "30-72727272-2", "Admin", "admin@otra-empresa.test", "Sup3rSecret!"));

        using var verifyContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true });
        Assert.Equal(1, verifyContext.Organizations.IgnoreQueryFilters().Count());
        Assert.False(verifyContext.Users.IgnoreQueryFilters().Any(u => u.Email == "admin@otra-empresa.test"));
    }

    [Fact]
    public async Task RegisterCompanyAsync_Throws_And_Rolls_Back_The_Organization_When_The_Admin_Email_Already_Exists()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();

        using (var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true }))
        {
            systemContext.Database.EnsureCreated();

            var existingOrg = new Organization { Name = "Org Previa", Cuit = "30-73737373-3" };
            systemContext.Organizations.Add(existingOrg);
            systemContext.SaveChanges();

            systemContext.Users.Add(new User
            {
                OrganizationId = existingOrg.Id,
                Email = "repetido@dominio.test",
                FullName = "Usuario Previo",
                PasswordHash = "hash",
                RoleId = RoleNames.ChoferId,
            });
            systemContext.SaveChanges();
        }

        var factory = new FakeDbContextFactory(connection, new FakeTenantProvider { IsSystemMode = true });
        ICompanyRegistrationService sut = new CompanyRegistrationService(factory, Hasher);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RegisterCompanyAsync(
            "Empresa Nueva Con Email Repetido SA", "30-74747474-4", "Admin", "repetido@dominio.test", "Sup3rSecret!"));

        using var verifyContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true });
        Assert.False(verifyContext.Organizations.IgnoreQueryFilters().Any(o => o.Cuit == "30-74747474-4"));
    }
}
