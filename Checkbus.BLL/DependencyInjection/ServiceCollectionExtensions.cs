using Checkbus.BEL.Auth;
using Checkbus.BLL.Auth;
using Checkbus.BLL.Organization;
using Checkbus.BLL.Tenancy;
using Checkbus.DAL.Context;
using Checkbus.DAL.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
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
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantProvider, HttpContextTenantProvider>();

        services.AddCheckbusPersistence(connectionString);

        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

        services.AddScoped<IEntitlementService, EntitlementService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IAuthorizationGuard, AuthorizationGuard>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICompanyRegistrationService, CompanyRegistrationService>();

        return services;
    }
}
