namespace Checkbus.BLL.Tenancy;

/// <summary>
/// Scoped (one per Blazor Server circuit) fallback for tenant resolution
/// inside interactive components. Microsoft Learn advises against relying on
/// <see cref="Microsoft.AspNetCore.Http.IHttpContextAccessor"/> during
/// interactive Blazor Server rendering because <c>HttpContext</c> is not
/// always available once the circuit is running past the initial render.
/// <see cref="HttpContextTenantProvider"/> falls back to this value when
/// <c>HttpContext</c> is unavailable. Populated exactly once, by convention
/// from a root layout (<c>MainLayout</c>) during its first render, from the
/// cascading <see cref="Microsoft.AspNetCore.Components.Authorization.AuthenticationState"/>
/// (which is captured once per circuit and does not depend on a live
/// <c>HttpContext</c> on every access).
/// </summary>
public class CircuitTenantState
{
    private bool _captured;

    public int? OrganizationId { get; private set; }

    /// <summary>
    /// Records the tenant for this circuit. Idempotent: only the first call
    /// takes effect, so repeated renders of the capturing layout don't
    /// overwrite the value with a possibly-stale later read.
    /// </summary>
    public void Capture(int? organizationId)
    {
        if (_captured)
        {
            return;
        }

        OrganizationId = organizationId;
        _captured = true;
    }
}
