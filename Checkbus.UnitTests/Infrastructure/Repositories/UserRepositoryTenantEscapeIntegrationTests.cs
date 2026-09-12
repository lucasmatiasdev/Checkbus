using Checkbus.Application.UseCases.Authentication.Login;
using Checkbus.Domain.Entities.Authentication;
using Checkbus.Domain.Entities.Authentication.Authorization;
using Checkbus.Domain.Entities.Tenancy;
using Checkbus.Infrastructure.Context;
using Checkbus.Infrastructure.Repositories;
using Checkbus.Infrastructure.Security;
using Checkbus.UnitTests.TestDoubles;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.UnitTests.Infrastructure.Repositories
{
    /// <summary>
    /// D7 (blocking): the D6 global tenant filter on User would make login impossible, because
    /// LoginUseCase resolves the user by email BEFORE any tenant is known. UserRepository.
    /// FindByEmailAsync (and FindEmailsByPrefixAsync) MUST bypass the filter via
    /// IgnoreQueryFilters(); this proves LoginUseCase succeeds against a real, tenant-filtered
    /// CheckbusDbContext bound to the fail-closed "no tenant" default.
    /// </summary>
    public class UserRepositoryTenantEscapeIntegrationTests : IAsyncDisposable
    {
        private static readonly DbContextOptions<CheckbusDbContext> Options =
            new DbContextOptionsBuilder<CheckbusDbContext>()
                .UseNpgsql(PostgresTestConnection.ConnectionString)
                .UseSnakeCaseNamingConvention()
                .Options;

        private const string PlainPassword = "correct-password";

        private readonly List<int> _seededOrganizationIds = new();

        [Fact]
        public async Task LoginUseCase_NoTenantBound_StillFindsUserAndSucceeds()
        {
            var (org, user) = await SeedOrganizationWithOneUserAsync();

            using var context = new CheckbusDbContext(Options);
            context.BindTenant(UnauthenticatedTenant.Instance);

            var userRepository = new UserRepository(context);
            var passwordHasher = new IdentityPasswordHasher();
            var sut = new LoginUseCase(userRepository, passwordHasher, TimeProvider.System);

            var result = await sut.ExecuteAsync(new LoginRequest(user.Email, PlainPassword));

            Assert.True(result.Success);
            Assert.Equal(user.Id, result.UserId);
            Assert.Equal(org.Id, result.OrganizationId);
        }

        [Fact]
        public async Task FindEmailsByPrefixAsync_NoTenantBound_StillReturnsMatchingEmails()
        {
            var (org, user) = await SeedOrganizationWithOneUserAsync();

            using var context = new CheckbusDbContext(Options);
            context.BindTenant(UnauthenticatedTenant.Instance);

            var userRepository = new UserRepository(context);
            var localPartPrefix = user.Email[..user.Email.IndexOf('@')];

            var emails = await userRepository.FindEmailsByPrefixAsync(org.Id, localPartPrefix);

            Assert.Contains(user.Email, emails);
        }

        private async Task<(Organization Org, User User)> SeedOrganizationWithOneUserAsync()
        {
            using var seedContext = new CheckbusDbContext(Options);

            var suffix = Guid.NewGuid().ToString("N")[..8];
            var org = new Organization { Name = $"D7-Org-{suffix}", CUIT = $"20-{suffix[..8]}-5" };
            seedContext.Organizations.Add(org);
            await seedContext.SaveChangesAsync();
            _seededOrganizationIds.Add(org.Id);

            var profile = new Profile { Name = $"D7-Profile-{suffix}", OrganizationId = org.Id };
            seedContext.Profiles.Add(profile);
            await seedContext.SaveChangesAsync();

            var user = new User
            {
                FullName = "D7 User",
                Email = $"d7-user-{suffix}@example.com",
                PasswordHash = new IdentityPasswordHasher().Hash(PlainPassword),
                Organization = org,
                OrganizationId = org.Id,
                Profile = profile,
                ProfileId = profile.Id,
                DocumentNumber = $"D7{suffix}"
            };
            seedContext.Users.Add(user);
            await seedContext.SaveChangesAsync();

            return (org, user);
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
