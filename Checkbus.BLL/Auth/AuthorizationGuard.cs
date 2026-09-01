namespace Checkbus.BLL.Auth;

/// <summary>
/// Default <see cref="IAuthorizationGuard"/>. The order is intentional and
/// tested: an entitlement denial short-circuits before <see cref="IPermissionService"/>
/// is ever evaluated, so a feature disabled for the organization's plan never
/// leaks information through a permission check.
/// </summary>
public class AuthorizationGuard : IAuthorizationGuard
{
    private readonly IEntitlementService _entitlementService;
    private readonly IPermissionService _permissionService;

    public AuthorizationGuard(IEntitlementService entitlementService, IPermissionService permissionService)
    {
        _entitlementService = entitlementService;
        _permissionService = permissionService;
    }

    public async Task<bool> AuthorizeAsync(int userId, int organizationId, string featureKey, string permissionKey)
    {
        var isEntitled = await _entitlementService.IsEntitledAsync(organizationId, featureKey);
        if (!isEntitled)
        {
            return false;
        }

        return await _permissionService.HasPermissionAsync(userId, permissionKey);
    }
}
