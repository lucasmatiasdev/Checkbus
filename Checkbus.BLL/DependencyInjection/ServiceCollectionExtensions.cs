using Checkbus.BEL.Auth;
using Checkbus.BLL.Auth;
using Checkbus.BLL.Fleet;
using Checkbus.BLL.Organization;
using Checkbus.BLL.Storage;
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
    /// <param name="fileStorageBasePath">
    /// Base directory <see cref="IFileStorageService"/> writes to. Environment-
    /// configurable (decision #41): a developer's own local disk during
    /// development, the VPS's local disk once deployed — only the value
    /// passed by the caller (<c>Checkbus.UI/Program.cs</c>, from
    /// configuration) changes.
    /// </param>
    public static IServiceCollection AddCheckbusApplication(
        this IServiceCollection services, string connectionString, string fileStorageBasePath)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<CircuitTenantState>();
        services.AddScoped<ITenantProvider, HttpContextTenantProvider>();

        services.AddCheckbusPersistence(connectionString);

        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddSingleton<IFileStorageService>(new LocalDiskFileStorageService(fileStorageBasePath));

        services.AddScoped<IEntitlementService, EntitlementService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IAuthorizationGuard, AuthorizationGuard>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICompanyRegistrationService, CompanyRegistrationService>();
        services.AddScoped<IFleetService, FleetService>();

        return services;
    }
}
