using Checkbus.BEL.Auth;
using Organization = Checkbus.BEL.Organization.Organization;

namespace Checkbus.Tests.Integration;

/// <summary>
/// S2.5 — the global tenant query filter must never leak cross-tenant rows
/// for a tenant-scoped entity, and a tenant-scoped entity's
/// <c>OrganizationId</c> must be immutable after insert.
/// </summary>
public class TenantFilterTests
{
    [Fact]
    public void Tenant_Scoped_Query_Never_Returns_Cross_Tenant_Users()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();

        int orgAId;
        int orgBId;

        using (var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true }))
        {
            systemContext.Database.EnsureCreated();

            var orgA = new Organization { Name = "Org A", Cuit = "30-11111111-1" };
            var orgB = new Organization { Name = "Org B", Cuit = "30-22222222-2" };
            systemContext.Organizations.AddRange(orgA, orgB);
            systemContext.SaveChanges();
            orgAId = orgA.Id;
            orgBId = orgB.Id;

            systemContext.Users.Add(new User
            {
                OrganizationId = orgAId,
                Email = "userA@org-a.test",
                FullName = "User A",
                PasswordHash = "hash-a",
                RoleId = RoleNames.ChoferId,
            });
            systemContext.Users.Add(new User
            {
                OrganizationId = orgBId,
                Email = "userB@org-b.test",
                FullName = "User B",
                PasswordHash = "hash-b",
                RoleId = RoleNames.ChoferId,
            });
            systemContext.SaveChanges();
        }

        using var tenantAContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { CurrentOrganizationId = orgAId });

        var visibleUsers = tenantAContext.Users.ToList();

        Assert.Single(visibleUsers);
        Assert.Equal("userA@org-a.test", visibleUsers[0].Email);
        Assert.All(visibleUsers, u => Assert.Equal(orgAId, u.OrganizationId));
    }

    [Fact]
    public void User_OrganizationId_Is_Immutable_After_Insert()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();

        int orgAId;
        int orgBId;
        int userId;

        using (var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true }))
        {
            systemContext.Database.EnsureCreated();

            var orgA = new Organization { Name = "Org A", Cuit = "30-33333333-3" };
            var orgB = new Organization { Name = "Org B", Cuit = "30-44444444-4" };
            systemContext.Organizations.AddRange(orgA, orgB);
            systemContext.SaveChanges();
            orgAId = orgA.Id;
            orgBId = orgB.Id;

            var user = new User
            {
                OrganizationId = orgAId,
                Email = "immutable@org-a.test",
                FullName = "Immutable User",
                PasswordHash = "hash",
                RoleId = RoleNames.ChoferId,
            };
            systemContext.Users.Add(user);
            systemContext.SaveChanges();
            userId = user.Id;
        }

        using var systemContext2 = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true });

        var trackedUser = systemContext2.Users.Single(u => u.Id == userId);
        trackedUser.OrganizationId = orgBId;

        Assert.Throws<InvalidOperationException>(() => systemContext2.SaveChanges());
    }
}
