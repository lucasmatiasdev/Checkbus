using Checkbus.BEL.Auth;
using Checkbus.DAL.Context;
using Microsoft.EntityFrameworkCore;
using Organization = Checkbus.BEL.Organization.Organization;

namespace Checkbus.Tests.Integration;

/// <summary>
/// S2.17 — <see cref="TenantScopedDbContextFactory"/> is the production
/// <c>IDbContextFactory&lt;CheckbusDbContext&gt;</c> registration: every
/// context it creates must be stamped with the injected
/// <see cref="ITenantProvider"/>, so the tenant query filter behaves
/// identically to a context created directly with
/// <c>SetTenantProvider</c> (the pattern used everywhere else in the test
/// suite).
/// </summary>
public class TenantScopedDbContextFactoryTests
{
    [Fact]
    public async Task CreateDbContextAsync_Stamps_The_Context_With_The_Injected_Tenant_Provider()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();
        int orgAId;
        int orgBId;

        using (var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true }))
        {
            systemContext.Database.EnsureCreated();

            var orgA = new Organization { Name = "Org A", Cuit = "30-81818181-1" };
            var orgB = new Organization { Name = "Org B", Cuit = "30-82828282-2" };
            systemContext.Organizations.AddRange(orgA, orgB);
            systemContext.SaveChanges();
            orgAId = orgA.Id;
            orgBId = orgB.Id;

            systemContext.Users.Add(new User
            {
                OrganizationId = orgAId,
                Email = "userA@tenant-factory.test",
                FullName = "User A",
                PasswordHash = "hash",
                RoleId = RoleNames.ChoferId,
            });
            systemContext.Users.Add(new User
            {
                OrganizationId = orgBId,
                Email = "userB@tenant-factory.test",
                FullName = "User B",
                PasswordHash = "hash",
                RoleId = RoleNames.ChoferId,
            });
            systemContext.SaveChanges();
        }

        var options = new DbContextOptionsBuilder<CheckbusDbContext>().UseSqlite(connection).Options;
        var tenantProvider = new FakeTenantProvider { CurrentOrganizationId = orgAId };
        var sut = new TenantScopedDbContextFactory(options, tenantProvider);

        await using var context = await sut.CreateDbContextAsync();
        var visibleUsers = context.Users.ToList();

        Assert.Single(visibleUsers);
        Assert.Equal("userA@tenant-factory.test", visibleUsers[0].Email);
    }
}
