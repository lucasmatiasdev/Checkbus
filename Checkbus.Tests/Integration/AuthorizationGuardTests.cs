using Checkbus.BLL.Auth;

namespace Checkbus.Tests.Integration;

/// <summary>
/// S2.13 — <see cref="IAuthorizationGuard"/> must check <see cref="IEntitlementService"/>
/// BEFORE <see cref="IPermissionService"/>: when a feature is entitlement-gated
/// off, permission is never even evaluated.
/// </summary>
public class AuthorizationGuardTests
{
    private class RecordingEntitlementService : IEntitlementService
    {
        private readonly bool _isEntitled;
        public bool WasCalled { get; private set; }

        public RecordingEntitlementService(bool isEntitled) => _isEntitled = isEntitled;

        public Task<bool> IsEntitledAsync(int organizationId, string featureKey)
        {
            WasCalled = true;
            return Task.FromResult(_isEntitled);
        }
    }

    private class RecordingPermissionService : IPermissionService
    {
        private readonly bool _hasPermission;
        public bool WasCalled { get; private set; }

        public RecordingPermissionService(bool hasPermission) => _hasPermission = hasPermission;

        public Task<bool> HasPermissionAsync(int userId, string permissionKey)
        {
            WasCalled = true;
            return Task.FromResult(_hasPermission);
        }
    }

    [Fact]
    public async Task Permission_Is_Never_Evaluated_When_Entitlement_Denies()
    {
        var entitlementService = new RecordingEntitlementService(isEntitled: false);
        var permissionService = new RecordingPermissionService(hasPermission: true);
        IAuthorizationGuard sut = new AuthorizationGuard(entitlementService, permissionService);

        var authorized = await sut.AuthorizeAsync(userId: 1, organizationId: 1, featureKey: "any.feature", permissionKey: "any.permission");

        Assert.False(authorized);
        Assert.True(entitlementService.WasCalled);
        Assert.False(permissionService.WasCalled);
    }

    [Fact]
    public async Task Permission_Is_Evaluated_When_Entitlement_Allows()
    {
        var entitlementService = new RecordingEntitlementService(isEntitled: true);
        var permissionService = new RecordingPermissionService(hasPermission: true);
        IAuthorizationGuard sut = new AuthorizationGuard(entitlementService, permissionService);

        var authorized = await sut.AuthorizeAsync(userId: 1, organizationId: 1, featureKey: "any.feature", permissionKey: "any.permission");

        Assert.True(authorized);
        Assert.True(entitlementService.WasCalled);
        Assert.True(permissionService.WasCalled);
    }
}
