namespace Checkbus.BLL.Auth;

/// <summary>
/// Resolves whether an organization is entitled to a given feature, based on
/// its subscribed plan. Checked BEFORE <see cref="IPermissionService"/> in the
/// guard order enforced by <see cref="IAuthorizationGuard"/>.
/// </summary>
public interface IEntitlementService
{
    /// <summary>
    /// Returns <c>true</c> when the organization identified by
    /// <paramref name="organizationId"/> is entitled to use the feature
    /// identified by <paramref name="featureKey"/>.
    /// </summary>
    Task<bool> IsEntitledAsync(int organizationId, string featureKey);
}
