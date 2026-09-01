namespace Checkbus.BLL.Auth;

/// <summary>
/// Semana 2 stub: every organization is entitled to every feature. Full
/// Plan/Feature-based enforcement is deferred to Semana 8 per the design;
/// this stub exists only so <see cref="IAuthorizationGuard"/> can already
/// enforce the entitlement-before-permission guard order today.
/// </summary>
public class EntitlementService : IEntitlementService
{
    public Task<bool> IsEntitledAsync(int organizationId, string featureKey) => Task.FromResult(true);
}
