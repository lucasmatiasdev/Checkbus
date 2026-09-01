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
    public static IServiceCollection AddCheckbusPersistence(this IServiceCollection services, string connectionString)
    {
        services.AddDbContextFactory<CheckbusDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}
