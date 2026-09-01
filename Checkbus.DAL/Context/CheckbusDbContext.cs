using Microsoft.EntityFrameworkCore;

namespace Checkbus.DAL.Context;

/// <summary>
/// Application database context for Checkbus. Shared-schema multi-tenancy is
/// enforced through <see cref="ITenantProvider"/>: tenant-scoped entities get a
/// global query filter on <c>OrganizationId</c>, and entities implementing
/// <see cref="IOptionallyTenantScoped"/> get a disjunctive filter (tenant match
/// OR global). No entities are registered yet — DbSets and the corresponding
/// <c>OnModelCreating</c> filter wiring arrive with the first business entity.
/// </summary>
public class CheckbusDbContext : DbContext
{
    private ITenantProvider? _tenantProvider;

    public CheckbusDbContext(DbContextOptions<CheckbusDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Optional tenant provider, set by the composition root after construction
    /// (property injection) so the design-time/migrations tooling only needs the
    /// single <see cref="DbContextOptions{TContext}"/> constructor.
    /// </summary>
    public void SetTenantProvider(ITenantProvider tenantProvider)
    {
        _tenantProvider = tenantProvider;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tenant query-filter wiring (ITenantScoped / IOptionallyTenantScoped)
        // is added per-entity as business entities are introduced in later weeks.
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Tenant-stamping hook: future weeks will set OrganizationId on added
        // ITenantScoped entities here before delegating to the base save.
        return base.SaveChangesAsync(cancellationToken);
    }
}
