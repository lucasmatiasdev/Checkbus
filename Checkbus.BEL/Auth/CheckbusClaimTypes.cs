namespace Checkbus.BEL.Auth;

/// <summary>
/// Custom claim type names issued at sign-in (CU-01) and read back by
/// <c>ITenantProvider</c> implementations to resolve the current tenant for a
/// request. Kept in BEL so both the UI (issuing claims) and BLL (reading
/// claims through the tenant provider) share the exact same literal.
/// </summary>
public static class CheckbusClaimTypes
{
    /// <summary>Carries the authenticated user's <c>OrganizationId</c>.</summary>
    public const string OrganizationId = "checkbus:organization_id";

    /// <summary>Carries the authenticated user's <c>RoleId</c>.</summary>
    public const string RoleId = "checkbus:role_id";
}
