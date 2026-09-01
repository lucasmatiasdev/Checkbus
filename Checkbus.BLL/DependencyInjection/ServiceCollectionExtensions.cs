using Checkbus.BLL.Auth;
using Checkbus.DAL.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Checkbus.BLL.DependencyInjection;

/// <summary>
/// Application composition root exposed to <c>Checkbus.UI</c>. This is the
/// only entry point the UI layer needs: it delegates persistence registration
/// to the DAL so the UI never takes a dependency on <c>Checkbus.DAL</c>.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCheckbusApplication(this IServiceCollection services, string connectionString)
    {
        services.AddCheckbusPersistence(connectionString);

        services.AddScoped<IEntitlementService, EntitlementService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IAuthorizationGuard, AuthorizationGuard>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}
