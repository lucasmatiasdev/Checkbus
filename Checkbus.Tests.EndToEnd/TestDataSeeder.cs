using Checkbus.BEL.Auth;
using Checkbus.DAL.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Organization = Checkbus.BEL.Organization.Organization;

namespace Checkbus.Tests.EndToEnd;

/// <summary>
/// Seeds one <see cref="Organization"/> and one Administrador
/// <see cref="User"/> directly against the factory's SQLite database, bypassing
/// the UI. This mirrors <c>CompanyRegistrationServiceTests</c>' direct-seed
/// pattern rather than driving <c>/company-registration</c> through Playwright
/// too: the object under test for S3.8 is tenant resolution INSIDE an
/// interactive circuit (<see cref="Checkbus.BLL.Tenancy.CircuitTenantState"/>),
/// not the signup flow, and CU-02 (company registration) already has its own
/// coverage in <c>Checkbus.Tests</c>. Driving login for real through Playwright
/// (see <c>CircuitTenantStateRealCircuitTests</c>) is what actually exercises
/// the real-cookie path this task needs.
/// </summary>
internal static class TestDataSeeder
{
    public static async Task<(int OrganizationId, string Email, string Password)> SeedOrganizationWithAdminAsync(
        CheckbusWebAppFactory factory)
    {
        const string email = "e2e-admin@checkbus.test";
        const string password = "Sup3rSecret!";

        await using var context = new CheckbusDbContext(factory.DbContextOptions);
        context.SetTenantProvider(new SystemModeTenantProvider());

        var organization = new Organization { Name = "E2E Test Org SA", Cuit = "30-99999999-9" };
        context.Organizations.Add(organization);
        await context.SaveChangesAsync();

        var hasher = new PasswordHasher<User>();
        var admin = new User
        {
            OrganizationId = organization.Id,
            Email = email,
            FullName = "Admin E2E",
            PasswordHash = hasher.HashPassword(null!, password),
            RoleId = RoleNames.AdministradorId,
        };
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        return (organization.Id, email, password);
    }

    public static async Task<Checkbus.BEL.Fleet.Driver?> FindDriverByFullNameAsync(
        CheckbusWebAppFactory factory, string fullName)
    {
        await using var context = new CheckbusDbContext(factory.DbContextOptions);
        context.SetTenantProvider(new SystemModeTenantProvider());

        return await context.Drivers.IgnoreQueryFilters()
            .SingleOrDefaultAsync(d => d.FullName == fullName);
    }
}
