namespace Checkbus.BEL.Auth;

/// <summary>
/// A named collection of permissions. A predefined role has
/// <see cref="OrganizationId"/> equal to <c>null</c> and is shared globally
/// across every tenant (see <see cref="RoleNames"/>). A custom role belongs to
/// exactly one organization and is only visible within that tenant.
/// </summary>
public class Role
{
    public int Id { get; set; }

    public required string Name { get; set; }

    /// <summary>
    /// <c>null</c> for the five predefined, globally shared roles;
    /// otherwise the organization that owns this custom role.
    /// </summary>
    public int? OrganizationId { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    public ICollection<User> Users { get; set; } = new List<User>();
}
