using Microsoft.EntityFrameworkCore;

namespace Checkbus.DAL.Context;

/// <summary>
/// Scoped <see cref="IDbContextFactory{TContext}"/> that stamps every
/// <see cref="CheckbusDbContext"/> it creates with the ambient (scoped)
/// <see cref="ITenantProvider"/>, so every context created within a request
/// enforces the correct tenant query filter. Registered instead of the plain
/// EF Core <c>AddDbContextFactory</c> singleton factory precisely because
/// <see cref="ITenantProvider"/> is request-scoped (it typically reads
/// <c>HttpContext.User</c>) while the built-in factory is a singleton and has
/// no notion of "current request".
/// </summary>
public class TenantScopedDbContextFactory : IDbContextFactory<CheckbusDbContext>
{
    private readonly DbContextOptions<CheckbusDbContext> _options;
    private readonly ITenantProvider _tenantProvider;

    public TenantScopedDbContextFactory(DbContextOptions<CheckbusDbContext> options, ITenantProvider tenantProvider)
    {
        _options = options;
        _tenantProvider = tenantProvider;
    }

    public CheckbusDbContext CreateDbContext()
    {
        var context = new CheckbusDbContext(_options);
        context.SetTenantProvider(_tenantProvider);
        return context;
    }

    public Task<CheckbusDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(CreateDbContext());
}
