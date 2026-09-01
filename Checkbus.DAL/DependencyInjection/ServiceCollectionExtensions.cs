using Checkbus.DAL.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Checkbus.DAL.DependencyInjection;

/// <summary>
/// DAL-side composition root. Registers persistence services (the EF Core
/// context factory and Npgsql provider) so upper layers never need a direct
/// reference to EF Core or connection-string details.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a scoped <see cref="IDbContextFactory{TContext}"/> backed by
    /// <see cref="TenantScopedDbContextFactory"/> rather than the plain EF Core
    /// <c>AddDbContextFactory</c> helper (which registers a singleton factory).
    /// <see cref="ITenantProvider"/> is resolved per-scope, so every
    /// <see cref="CheckbusDbContext"/> created during a request/operation is
    /// stamped with the caller's actual current tenant.
    /// </summary>
    public static IServiceCollection AddCheckbusPersistence(this IServiceCollection services, string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CheckbusDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        services.AddSingleton(optionsBuilder.Options);

        services.AddScoped<IDbContextFactory<CheckbusDbContext>, TenantScopedDbContextFactory>();

        return services;
    }
}
