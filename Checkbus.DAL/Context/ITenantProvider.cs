namespace Checkbus.DAL.Context;

/// <summary>
/// Resolves the tenant (organization) context for the current unit of work.
/// Implemented in the hosting layer (e.g. from the authenticated user's claims)
/// and consumed by <see cref="CheckbusDbContext"/> to enforce shared-schema
/// multi-tenancy via a global query filter on tenant-scoped entities.
/// </summary>
public interface ITenantProvider
{
    /// <summary>
    /// The organization identifier for the current request/operation, or
    /// <c>null</c> when there is no tenant context (e.g. before sign-in).
    /// </summary>
    int? CurrentOrganizationId { get; }

    /// <summary>
    /// When <c>true</c>, the current execution runs in "system mode" (for
    /// example a background job) and the tenant query filter must be bypassed
    /// rather than enforced against <see cref="CurrentOrganizationId"/>.
    /// </summary>
    bool IsSystemMode { get; }
}
