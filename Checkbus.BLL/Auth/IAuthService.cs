namespace Checkbus.BLL.Auth;

/// <summary>
/// The authenticated user's identity data needed to sign in (build the
/// claims principal for CU-01): tenant (<see cref="OrganizationId"/>) and
/// role, so the caller never needs a second round trip to the database.
/// </summary>
public record AuthenticatedUser(
    int UserId,
    int OrganizationId,
    string Email,
    string FullName,
    int RoleId,
    string RoleName);

/// <summary>
/// Login by e-mail only, with no tenant selector (CU-01): the user's own
/// record is the source of truth for which organization they belong to.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Verifies <paramref name="email"/>/<paramref name="password"/> against
    /// the stored user record. Returns <c>null</c> when there is no match
    /// (unknown e-mail, wrong password, or an inactive user) rather than
    /// distinguishing the reason, so callers cannot use response shape to
    /// enumerate valid e-mails.
    /// </summary>
    Task<AuthenticatedUser?> AuthenticateAsync(string email, string password);
}
