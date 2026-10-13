using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Payments;
using Checkbus.ApiService.Application.Payments.Commands;

namespace Checkbus.Tests.Payments;

/// <summary>
/// Proves <see cref="StartMercadoPagoConnectionCommandHandler"/> (odd/tasks/pagos-mercadopago.md
/// Fase I, I7): the caller's own OrganizationId (never a request value) is used both to build
/// the state and to call <see cref="IMercadoPagoOAuthService.BuildAuthorizationUrl"/>, and a
/// missing OrganizationId claim throws — same anti-IDOR convention as
/// <c>GetVehiclesQueryHandlerTests</c>. Fake-service style, no live Mercado Pago credentials.
/// </summary>
public class StartMercadoPagoConnectionCommandHandlerTests
{
    private sealed class FakeCurrentUserService(Guid? organizationId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.NewGuid();
        public Guid? OrganizationId => organizationId;
        public string? Role => "Administrador";
        public string? Email => "admin@checkbus.test";
    }

    private sealed class FakeOAuthService(string authorizationUrl) : IMercadoPagoOAuthService
    {
        public Guid? ReceivedOrganizationId { get; private set; }
        public string? ReceivedState { get; private set; }

        public string BuildAuthorizationUrl(Guid organizationId, string state)
        {
            ReceivedOrganizationId = organizationId;
            ReceivedState = state;
            return authorizationUrl;
        }

        public Task<MercadoPagoOAuthTokenResult> ExchangeCodeAsync(string code, string state, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by StartMercadoPagoConnectionCommandHandler.");

        public Task<MercadoPagoOAuthTokenResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by StartMercadoPagoConnectionCommandHandler.");
    }

    private sealed class FakeStateService(string protectedState) : IMercadoPagoConnectionStateService
    {
        public Guid? ReceivedOrganizationId { get; private set; }

        public string Protect(Guid organizationId)
        {
            ReceivedOrganizationId = organizationId;
            return protectedState;
        }

        public MercadoPagoConnectionStateResult TryUnprotect(string protectedState)
            => throw new NotSupportedException("Not needed by StartMercadoPagoConnectionCommandHandler.");
    }

    [Fact]
    public async Task Handle_BuildsStateAndCallsBuildAuthorizationUrl_WithCallersOwnOrganizationId()
    {
        var organizationId = Guid.NewGuid();
        var currentUser = new FakeCurrentUserService(organizationId);
        var oauthService = new FakeOAuthService("https://auth.mercadopago.com/authorization?client_id=APP-123");
        var stateService = new FakeStateService("protected-state-value");

        var handler = new StartMercadoPagoConnectionCommandHandler(currentUser, oauthService, stateService);

        var result = await handler.Handle(new StartMercadoPagoConnectionCommand(), TestContext.Current.CancellationToken);

        Assert.Equal(organizationId, stateService.ReceivedOrganizationId);
        Assert.Equal(organizationId, oauthService.ReceivedOrganizationId);
        Assert.Equal("protected-state-value", oauthService.ReceivedState);
        Assert.Equal("https://auth.mercadopago.com/authorization?client_id=APP-123", result.AuthorizationUrl);
    }

    [Fact]
    public async Task Handle_NoOrganizationIdClaim_ThrowsInvalidOperation()
    {
        var currentUser = new FakeCurrentUserService(organizationId: null);
        var handler = new StartMercadoPagoConnectionCommandHandler(
            currentUser, new FakeOAuthService("unused"), new FakeStateService("unused"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new StartMercadoPagoConnectionCommand(), TestContext.Current.CancellationToken));
    }
}
