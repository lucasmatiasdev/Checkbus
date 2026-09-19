namespace Checkbus.Application.Abstractions
{
    /// <summary>
    /// Synchronously projects the current authenticated tenant's OrganizationId for use inside
    /// EF Core global query filter closures (see design D1/D5). `null` is the explicit "no tenant"
    /// state (migrations, seeds, anonymous SSR, background work) — never an exception.
    /// </summary>
    public interface ICurrentTenant
    {
        int? OrganizationId { get; }

        /// <summary>
        /// The current tenant's organization name. `null` is the explicit "no tenant" state,
        /// same contract as <see cref="OrganizationId"/> — never an exception.
        /// </summary>
        string? OrganizationName { get; }
    }
}
