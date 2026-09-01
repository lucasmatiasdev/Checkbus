namespace Checkbus.BEL.Auth;

/// <summary>
/// An authenticated application user. Always belongs to exactly one
/// organization (tenant-scoped, non-nullable <see cref="OrganizationId"/>) and
/// always has exactly one <see cref="Role"/> (predefined or custom). Login is
/// by <see cref="Email"/> alone (globally unique, no tenant selector per CU-01).
/// </summary>
public class User
{
    public int Id { get; set; }

    public int OrganizationId { get; set; }

    public required string Email { get; set; }

    public required string FullName { get; set; }

    public required string PasswordHash { get; set; }

    public int RoleId { get; set; }

    public Role Role { get; set; } = null!;

    public bool IsActive { get; set; } = true;
}
