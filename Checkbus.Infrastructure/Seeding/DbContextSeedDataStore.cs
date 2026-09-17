using Checkbus.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Infrastructure.Seeding
{
    /// <summary>
    /// EF Core adapter for <see cref="ISeedDataStore"/>. Uses <see cref="IDbContextFactory{TContext}"/>
    /// directly rather than the scoped <see cref="CheckbusDbContext"/> registration, because that
    /// scoped registration resolves <c>ICurrentTenant</c> to the Blazor-circuit-bound
    /// <c>CircuitCurrentTenant</c>, which is meaningless at application boot.
    /// </summary>
    public sealed class DbContextSeedDataStore : ISeedDataStore
    {
        private readonly IDbContextFactory<CheckbusDbContext> _contextFactory;

        public DbContextSeedDataStore(IDbContextFactory<CheckbusDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<bool> HasAnyOrganizationAsync(CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            // D10: no IgnoreQueryFilters() here — Organization implements neither ITenantEntity nor
            // IOptionalTenantEntity, and OrganizationConfiguration adds no HasQueryFilter, so
            // Organization carries no tenant filter to bypass. Adding the call would be a
            // misleading no-op implying a filter exists (see design decision "Organizations.
            // AnyAsync() WITHOUT IgnoreQueryFilters()"); the integration test for this store fails
            // the day an Organization filter is ever added.
            return await context.Organizations.AnyAsync(cancellationToken);
        }

        public async Task PersistAsync(SeedGraph graph, CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            context.Roles.AddRange(graph.Roles);
            context.Profiles.AddRange(graph.Profiles);
            context.Organizations.AddRange(graph.Organizations);
            context.Users.AddRange(graph.Users);

            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
