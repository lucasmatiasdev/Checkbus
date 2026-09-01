namespace Checkbus.BLL.Auth;

/// <summary>
/// Application service for <c>User</c> management, including role
/// assignment. Enforces the role-assignment scoping invariant: a user can
/// only be assigned a role that is global or belongs to the user's own
/// organization (see <see cref="UserService"/> for the check).
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Assigns the role identified by <paramref name="roleId"/> to the user
    /// identified by <paramref name="userId"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the role belongs to a different organization than the user.
    /// </exception>
    Task AssignRoleAsync(int userId, int roleId);
}
