namespace Checkbus.BLL.Auth;

/// <summary>
/// Resolves a user's effective permissions through their assigned
/// <c>Role</c>. Checked AFTER <see cref="IEntitlementService"/> in the guard
/// order enforced by <see cref="IAuthorizationGuard"/>: a plan/feature gate
/// always wins before an individual permission grant is even evaluated.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Returns <c>true</c> when the user identified by <paramref name="userId"/>
    /// has a role granting the given <paramref name="permissionKey"/> (see
    /// <see cref="Checkbus.BEL.Auth.PermissionKeys"/>).
    /// </summary>
    Task<bool> HasPermissionAsync(int userId, string permissionKey);
}
