namespace Checkbus.DAL.Context;

/// <summary>
/// Marker contract for entities that belong to exactly one organization and
/// must always be filtered by <see cref="ITenantProvider.CurrentOrganizationId"/>
/// (shared-schema multi-tenancy).
/// </summary>
public interface ITenantScoped
{
    int OrganizationId { get; set; }
}
