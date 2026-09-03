using System.Security.Claims;
using Checkbus.BEL.Auth;
using Checkbus.DAL.Context;
using Microsoft.AspNetCore.Http;

namespace Checkbus.BLL.Tenancy;

/// <summary>
/// Production <see cref="ITenantProvider"/>: reads the current tenant from
/// the <see cref="CheckbusClaimTypes.OrganizationId"/> claim on
/// <c>HttpContext.User</c>, as issued at sign-in (CU-01). Registered scoped
/// (one per request) so <see cref="TenantScopedDbContextFactory"/> stamps
/// every <c>CheckbusDbContext</c> created during that request with the
/// signed-in user's actual organization.
/// </summary>
public class HttpContextTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CircuitTenantState _circuitTenantState;

    public HttpContextTenantProvider(IHttpContextAccessor httpContextAccessor, CircuitTenantState circuitTenantState)
    {
        _httpContextAccessor = httpContextAccessor;
        _circuitTenantState = circuitTenantState;
    }

    public int? CurrentOrganizationId
    {
        get
        {
            var claimValue = _httpContextAccessor.HttpContext?.User?
                .FindFirst(CheckbusClaimTypes.OrganizationId)?.Value;

            if (claimValue is not null && int.TryParse(claimValue, out var organizationId))
            {
                return organizationId;
            }

            // HttpContext is unreliable deep inside an interactive Blazor
            // Server circuit (past the initial render) — fall back to the
            // value captured once at first render by CircuitTenantState.
            return _circuitTenantState.OrganizationId;
        }
    }

    /// <summary>
    /// Always <c>false</c> for the request pipeline: system mode is reserved
    /// for explicit background/seeding code paths, never for an
    /// HTTP-request-bound tenant provider.
    /// </summary>
    public bool IsSystemMode => false;
}
