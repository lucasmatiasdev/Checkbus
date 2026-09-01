namespace Checkbus.BEL.Auth;

/// <summary>
/// A single, atomic capability that can be granted to a <see cref="Role"/>.
/// Permissions are transversal: the same permission (e.g. <c>reports.view</c>)
/// can be granted to more than one predefined role. See <see cref="PermissionKeys"/>
/// for the fixed catalog of keys.
/// </summary>
public class Permission
{
    public int Id { get; set; }

    /// <summary>
    /// Stable <c>module.action</c> key (e.g. <c>reports.view</c>), matching one
    /// of the constants in <see cref="PermissionKeys"/>.
    /// </summary>
    public required string Key { get; set; }

    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
