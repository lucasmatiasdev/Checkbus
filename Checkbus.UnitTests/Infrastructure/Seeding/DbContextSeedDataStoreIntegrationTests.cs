using Checkbus.Domain.Entities.Authentication;
using Checkbus.Domain.Entities.Authentication.Authorization;
using Checkbus.Domain.Entities.Tenancy;
using Checkbus.Infrastructure.Context;
using Checkbus.Infrastructure.Seeding;
using Checkbus.UnitTests.TestDoubles;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.UnitTests.Infrastructure.Seeding
{
    /// <summary>
    /// Real-Postgres coverage for <see cref="DbContextSeedDataStore"/>. Uses throwaway
    /// <c>Guid</c>-suffixed graphs (never <see cref="DevelopmentSeedData"/>) so the fixed seed
    /// CUITs used by the real Development seed never collide with test data, and cleans up in
    /// <see cref="DisposeAsync"/> like the other real-Postgres integration tests in this project.
    /// </summary>
    public class DbContextSeedDataStoreIntegrationTests : IAsyncDisposable
    {
        private static readonly DbContextOptions<CheckbusDbContext> Options =
            new DbContextOptionsBuilder<CheckbusDbContext>()
                .UseNpgsql(PostgresTestConnection.ConnectionString)
                .UseSnakeCaseNamingConvention()
                .Options;

        private readonly List<int> _seededOrganizationIds = new();

        private static DbContextSeedDataStore CreateSut() => new(new TestDbContextFactory());

        /// <summary>
        /// Minimal <see cref="IDbContextFactory{TContext}"/> for this test: a fresh, never-tenant-
        /// bound <see cref="CheckbusDbContext"/> per call, matching how <see cref="DbContextSeedDataStore"/>
        /// is really wired via AddDbContextFactory in DependencyInjection.cs.
        /// </summary>
        private sealed class TestDbContextFactory : IDbContextFactory<CheckbusDbContext>
        {
            public CheckbusDbContext CreateDbContext() => new(Options);
        }

        [Fact]
        public async Task HasAnyOrganizationAsync_OrgExistsUnderUnboundTenant_ReturnsTrue()
        {
            var org = await SeedOneOrganizationAsync();
            var sut = CreateSut();

            // The store's factory-created context is never bound to a tenant (defaults to
            // UnauthenticatedTenant.Instance, D5). Organization carries no tenant filter to begin
            // with, so this must see the row regardless — this test goes red the day an
            // Organization query filter is ever added (D10).
            var hasAny = await sut.HasAnyOrganizationAsync();

            Assert.True(hasAny);
            Assert.True(org.Id > 0);
        }

        [Fact]
        public async Task PersistAsync_FullGraph_InsertsOrganizationsProfilesRolesAndUsersInOneCall()
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var role = new Role { Name = $"SeedStore-Role-{suffix}" };
            var org = new Organization { Name = $"SeedStore-Org-{suffix}", CUIT = $"20-{suffix}-9" };
            var profile = new Profile
            {
                Name = $"SeedStore-Profile-{suffix}",
                Organization = org,
                Roles = new List<Role> { role }
            };
            var user = new User
            {
                FullName = "SeedStore Test User",
                Email = $"seedstore-{suffix}@example.com",
                PasswordHash = "irrelevant-for-this-test",
                Organization = org,
                Profile = profile,
                DocumentNumber = $"SS{suffix}"
            };
            var graph = new SeedGraph(
                Roles: new List<Role> { role },
                Profiles: new List<Profile> { profile },
                Organizations: new List<Organization> { org },
                Users: new List<User> { user });

            var sut = CreateSut();
            await sut.PersistAsync(graph);

            using var verifyContext = new CheckbusDbContext(Options);
            var insertedOrg = await verifyContext.Organizations
                .SingleAsync(o => o.CUIT == org.CUIT);
            _seededOrganizationIds.Add(insertedOrg.Id);

            var insertedUsers = await verifyContext.Users.IgnoreQueryFilters()
                .Where(u => u.OrganizationId == insertedOrg.Id)
                .ToListAsync();
            var insertedProfiles = await verifyContext.Profiles.IgnoreQueryFilters()
                .Where(p => p.OrganizationId == insertedOrg.Id)
                .ToListAsync();

            Assert.Single(insertedUsers);
            Assert.Equal(user.Email, insertedUsers[0].Email);
            Assert.Single(insertedProfiles);
            Assert.Equal(profile.Name, insertedProfiles[0].Name);
        }

        private async Task<Organization> SeedOneOrganizationAsync()
        {
            using var seedContext = new CheckbusDbContext(Options);

            var suffix = Guid.NewGuid().ToString("N")[..8];
            var org = new Organization { Name = $"SeedStore-Guard-{suffix}", CUIT = $"20-{suffix}-8" };
            seedContext.Organizations.Add(org);
            await seedContext.SaveChangesAsync();
            _seededOrganizationIds.Add(org.Id);

            return org;
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
