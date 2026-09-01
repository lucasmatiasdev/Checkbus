namespace Checkbus.DAL.Context;

/// <summary>
/// Marker contract for entities that may belong to a single organization or be
/// shared globally (nullable <see cref="OrganizationId"/>). These entities use
/// a disjunctive query filter: visible when they match the current tenant OR
/// when they are global (<c>OrganizationId == null</c>). Used later by entities
/// such as <c>Role</c>.
/// </summary>
public interface IOptionallyTenantScoped
{
    int? OrganizationId { get; set; }
}
