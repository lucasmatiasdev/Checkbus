namespace Checkbus.BEL.Auth;

/// <summary>
/// Join entity granting a <see cref="Permission"/> to a <see cref="Role"/>.
/// Composite primary key on (<see cref="RoleId"/>, <see cref="PermissionId"/>).
/// </summary>
public class RolePermission
{
    public int RoleId { get; set; }

    public Role Role { get; set; } = null!;

    public int PermissionId { get; set; }

    public Permission Permission { get; set; } = null!;
}
