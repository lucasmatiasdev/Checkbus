using Checkbus.Domain.Entities.Authentication;
using Checkbus.Domain.Entities.Authentication.Authorization;
using Checkbus.Domain.Entities.Tenancy;
using Checkbus.Infrastructure.Context;
using Checkbus.UnitTests.TestDoubles;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.UnitTests.Infrastructure.Context
{
    /// <summary>
    /// D9 (blocking): proves the D4 instance-field query filter pattern actually re-evaluates per
    /// CheckbusDbContext instance against a real Postgres database, not just the compiled model
    /// once. Two separate context instances sharing the same DbContextOptions (so they share EF's
    /// cached model) must see disjoint User rows for two different tenants, and zero rows for the
    /// fail-closed "no tenant" default.
    /// </summary>
    public class TenantQueryFilterIntegrationTests : IAsyncDisposable
    {
        private static readonly DbContextOptions<CheckbusDbContext> Options =
            new DbContextOptionsBuilder<CheckbusDbContext>()
                .UseNpgsql(PostgresTestConnection.ConnectionString)
                .UseSnakeCaseNamingConvention()
                .Options;

        private readonly List<int> _seededOrganizationIds = new();

        [Fact]
        public async Task Query_TwoTenantsSameModel_ReturnsDisjointUserRowsAndNullTenantReturnsZero()
        {
            var (orgA, orgB, userA, userB) = await SeedTwoTenantsWithOneUserEachAsync();

            using var contextA = new CheckbusDbContext(Options);
            contextA.BindTenant(new FixedTenant(orgA.Id));

            using var contextB = new CheckbusDbContext(Options);
            contextB.BindTenant(new FixedTenant(orgB.Id));

            using var contextNoTenant = new CheckbusDbContext(Options);
            contextNoTenant.BindTenant(UnauthenticatedTenant.Instance);

            var seededIds = new[] { userA.Id, userB.Id };

            var seenByA = await contextA.Users.Where(u => seededIds.Contains(u.Id)).ToListAsync();
            var seenByB = await contextB.Users.Where(u => seededIds.Contains(u.Id)).ToListAsync();
            var seenByNoTenant = await contextNoTenant.Users.Where(u => seededIds.Contains(u.Id)).ToListAsync();

            Assert.Single(seenByA);
            Assert.Equal(userA.Id, seenByA[0].Id);

            Assert.Single(seenByB);
            Assert.Equal(userB.Id, seenByB[0].Id);

            Assert.Empty(seenByNoTenant);

            // Independent confirmation bypassing the filter: both rows genuinely exist, so the
            // empty/single results above prove filtering, not accidental missing data.
            var bypassed = await contextNoTenant.Users.IgnoreQueryFilters()
                .Where(u => seededIds.Contains(u.Id))
                .ToListAsync();
            Assert.Equal(2, bypassed.Count);
        }

        private async Task<(Organization OrgA, Organization OrgB, User UserA, User UserB)> SeedTwoTenantsWithOneUserEachAsync()
        {
            using var seedContext = new CheckbusDbContext(Options);

            var suffix = Guid.NewGuid().ToString("N")[..8];
            var orgA = new Organization { Name = $"D9-OrgA-{suffix}", CUIT = $"20-{suffix[..8]}-1" };
            var orgB = new Organization { Name = $"D9-OrgB-{suffix}", CUIT = $"20-{suffix[..8]}-2" };
            seedContext.Organizations.AddRange(orgA, orgB);
            await seedContext.SaveChangesAsync();
            _seededOrganizationIds.Add(orgA.Id);
            _seededOrganizationIds.Add(orgB.Id);

            var profileA = new Profile { Name = $"D9-ProfileA-{suffix}", OrganizationId = orgA.Id };
            var profileB = new Profile { Name = $"D9-ProfileB-{suffix}", OrganizationId = orgB.Id };
            seedContext.Profiles.AddRange(profileA, profileB);
            await seedContext.SaveChangesAsync();

            var userA = new User
            {
                FullName = "D9 User A",
                Email = $"d9-user-a-{suffix}@example.com",
                PasswordHash = "irrelevant-for-this-test",
                Organization = orgA,
                OrganizationId = orgA.Id,
                Profile = profileA,
                ProfileId = profileA.Id,
                DocumentNumber = $"A{suffix}"
            };
            var userB = new User
            {
                FullName = "D9 User B",
                Email = $"d9-user-b-{suffix}@example.com",
                PasswordHash = "irrelevant-for-this-test",
                Organization = orgB,
                OrganizationId = orgB.Id,
                Profile = profileB,
                ProfileId = profileB.Id,
                DocumentNumber = $"B{suffix}"
            };
            seedContext.Users.AddRange(userA, userB);
            await seedContext.SaveChangesAsync();

            return (orgA, orgB, userA, userB);
        }

        public async ValueTask DisposeAsync()
        {
            if (_seededOrganizationIds.Count == 0)
            {
                return;
            }

            using var cleanupContext = new CheckbusDbContext(Options);
            var orgIds = _seededOrganizationIds;

            var users = await cleanupContext.Users.IgnoreQueryFilters()
                .Where(u => orgIds.Contains(u.OrganizationId))
                .ToListAsync();
            cleanupContext.Users.RemoveRange(users);

            var profiles = await cleanupContext.Profiles.IgnoreQueryFilters()
                .Where(p => p.OrganizationId != null && orgIds.Contains(p.OrganizationId.Value))
                .ToListAsync();
            cleanupContext.Profiles.RemoveRange(profiles);

            var organizations = await cleanupContext.Organizations
                .Where(o => orgIds.Contains(o.Id))
                .ToListAsync();
            cleanupContext.Organizations.RemoveRange(organizations);

            await cleanupContext.SaveChangesAsync();
        }
    }
}
