namespace Checkbus.BLL.Auth;

/// <summary>
/// Combined authorization guard used by BLL feature methods. Enforces the
/// fixed guard order: <see cref="IEntitlementService"/> first, then
/// <see cref="IPermissionService"/> only when the feature is entitled.
/// </summary>
public interface IAuthorizationGuard
{
    Task<bool> AuthorizeAsync(int userId, int organizationId, string featureKey, string permissionKey);
}
