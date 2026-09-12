using Checkbus.Domain.Entities.Authentication.Authorization;
using Checkbus.Domain.Entities.Tenancy;
using Checkbus.Infrastructure.Context;
using Checkbus.UnitTests.TestDoubles;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.UnitTests.Infrastructure.Context
{
    /// <summary>
    /// Proves the D6 IOptionalTenantEntity filter on Profile: a null-OrganizationId profile (a
    /// "global" profile) is visible to any authenticated tenant, while another organization's
    /// non-null-OrganizationId profile is excluded. Real Postgres, real query filter.
    /// </summary>
    public class ProfileOptionalTenantFilterIntegrationTests : IAsyncDisposable
    {
        private static readonly DbContextOptions<CheckbusDbContext> Options =
            new DbContextOptionsBuilder<CheckbusDbContext>()
                .UseNpgsql(PostgresTestConnection.ConnectionString)
                .UseSnakeCaseNamingConvention()
                .Options;

        private readonly List<int> _seededOrganizationIds = new();
        private readonly List<int> _seededProfileIds = new();

        [Fact]
        public async Task Query_GlobalAndOtherOrgProfiles_ReturnsGlobalButExcludesOtherOrg()
        {
            var (orgA, orgB, globalProfile, orgBProfile) = await SeedGlobalAndOrgBProfilesAsync();
            var seededIds = new[] { globalProfile.Id, orgBProfile.Id };

            using var contextA = new CheckbusDbContext(Options);
            contextA.BindTenant(new FixedTenant(orgA.Id));

            var seenByA = await contextA.Profiles
                .Where(p => seededIds.Contains(p.Id))
                .ToListAsync();

            Assert.Single(seenByA);
            Assert.Equal(globalProfile.Id, seenByA[0].Id);

            // Independent confirmation bypassing the filter: orgB's profile genuinely exists, so
            // its exclusion above proves filtering, not accidental missing data.
            using var contextNoTenant = new CheckbusDbContext(Options);
            contextNoTenant.BindTenant(UnauthenticatedTenant.Instance);
            var bypassed = await contextNoTenant.Profiles.IgnoreQueryFilters()
                .Where(p => seededIds.Contains(p.Id))
                .ToListAsync();
            Assert.Equal(2, bypassed.Count);
        }

        private async Task<(Organization OrgA, Organization OrgB, Profile GlobalProfile, Profile OrgBProfile)> SeedGlobalAndOrgBProfilesAsync()
        {
            using var seedContext = new CheckbusDbContext(Options);

            var suffix = Guid.NewGuid().ToString("N")[..8];
            var orgA = new Organization { Name = $"D6-OrgA-{suffix}", CUIT = $"20-{suffix[..8]}-3" };
            var orgB = new Organization { Name = $"D6-OrgB-{suffix}", CUIT = $"20-{suffix[..8]}-4" };
            seedContext.Organizations.AddRange(orgA, orgB);
            await seedContext.SaveChangesAsync();
            _seededOrganizationIds.Add(orgA.Id);
            _seededOrganizationIds.Add(orgB.Id);

            var globalProfile = new Profile { Name = $"D6-GlobalProfile-{suffix}", OrganizationId = null };
            var orgBProfile = new Profile { Name = $"D6-OrgBProfile-{suffix}", OrganizationId = orgB.Id };
            seedContext.Profiles.AddRange(globalProfile, orgBProfile);
            await seedContext.SaveChangesAsync();
            _seededProfileIds.Add(globalProfile.Id);
            _seededProfileIds.Add(orgBProfile.Id);

            return (orgA, orgB, globalProfile, orgBProfile);
        }

        public async ValueTask DisposeAsync()
        {
            if (_seededProfileIds.Count == 0 && _seededOrganizationIds.Count == 0)
            {
                return;
            }

            using var cleanupContext = new CheckbusDbContext(Options);
            var profileIds = _seededProfileIds;
            var orgIds = _seededOrganizationIds;

            var profiles = await cleanupContext.Profiles.IgnoreQueryFilters()
                .Where(p => profileIds.Contains(p.Id))
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
