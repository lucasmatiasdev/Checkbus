using Checkbus.ApiService.Application.Interfaces.Payments;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Payments;
using Checkbus.ApiService.Domain.Entities.Payments;
using Checkbus.ApiService.Domain.Exceptions.Payments;

namespace Checkbus.Tests.Payments;

/// <summary>
/// Proves <see cref="MercadoPagoSellerTokenResolver"/> (odd/tasks/pagos-mercadopago.md Fase I,
/// I6): no connection throws <see cref="OrganizationNotConnectedToMercadoPagoException"/>; a
/// still-valid connection returns its AccessToken as-is without ever calling
/// <see cref="IMercadoPagoOAuthService.RefreshAsync"/>; a connection expired or within the
/// refresh buffer calls RefreshAsync, persists the ROTATED refresh token (never the old one)
/// via <see cref="IOrganizationMercadoPagoConnectionRepository.UpsertAsync"/>, and returns the
/// new access token. Fake-repository/fake-service style, matching
/// <see cref="Checkbus.Tests.Trips.HandlePaymentWebhookCommandHandlerTests"/>. No live Mercado
/// Pago credentials are needed or used — IMercadoPagoOAuthService is faked entirely.
/// </summary>
public class MercadoPagoSellerTokenResolverTests
{
    private sealed class FakeConnectionRepository(MercadoPagoConnection? connection) : IOrganizationMercadoPagoConnectionRepository
    {
        public int UpsertCallCount { get; private set; }
        public MercadoPagoConnection? LastUpserted { get; private set; }

        public Task<MercadoPagoConnection?> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken)
            => Task.FromResult(connection is not null && connection.OrganizationId == organizationId ? connection : null);

        public Task UpsertAsync(MercadoPagoConnection connection, CancellationToken cancellationToken)
        {
            UpsertCallCount++;
            LastUpserted = connection;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeOAuthService(MercadoPagoOAuthTokenResult? refreshResult) : IMercadoPagoOAuthService
    {
        public int RefreshCallCount { get; private set; }
        public string? LastRefreshTokenPassedIn { get; private set; }

        public string BuildAuthorizationUrl(Guid organizationId, string state)
            => throw new NotSupportedException("Not needed by MercadoPagoSellerTokenResolver.");

        public Task<MercadoPagoOAuthTokenResult> ExchangeCodeAsync(string code, string state, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by MercadoPagoSellerTokenResolver.");

        public Task<MercadoPagoOAuthTokenResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
        {
            RefreshCallCount++;
            LastRefreshTokenPassedIn = refreshToken;
            return Task.FromResult(refreshResult!);
        }
    }

    private static MercadoPagoConnection CreateConnection(Guid organizationId, DateTime expiresAt) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = organizationId,
        SellerUserId = "111",
        AccessToken = "old-access-token",
        RefreshToken = "old-refresh-token",
        ExpiresAt = expiresAt,
        ConnectedAt = DateTime.UtcNow.AddDays(-1)
    };

    [Fact]
    public async Task ResolveAccessTokenAsync_NoConnection_ThrowsOrganizationNotConnected()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var connectionRepository = new FakeConnectionRepository(connection: null);
        var oauthService = new FakeOAuthService(refreshResult: null);
        var resolver = new MercadoPagoSellerTokenResolver(connectionRepository, oauthService);

        await Assert.ThrowsAsync<OrganizationNotConnectedToMercadoPagoException>(
            () => resolver.ResolveAccessTokenAsync(Guid.NewGuid(), cancellationToken));

        Assert.Equal(0, oauthService.RefreshCallCount);
    }

    [Fact]
    public async Task ResolveAccessTokenAsync_StillValidWellBeyondBuffer_ReturnsExistingTokenWithoutRefreshing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var organizationId = Guid.NewGuid();
        var connection = CreateConnection(organizationId, DateTime.UtcNow.AddDays(170));
        var connectionRepository = new FakeConnectionRepository(connection);
        var oauthService = new FakeOAuthService(refreshResult: null);
        var resolver = new MercadoPagoSellerTokenResolver(connectionRepository, oauthService);

        var token = await resolver.ResolveAccessTokenAsync(organizationId, cancellationToken);

        Assert.Equal("old-access-token", token);
        Assert.Equal(0, oauthService.RefreshCallCount);
        Assert.Equal(0, connectionRepository.UpsertCallCount);
    }

    [Fact]
    public async Task ResolveAccessTokenAsync_WithinExpirationBuffer_RefreshesAndPersistsRotatedPair()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var organizationId = Guid.NewGuid();
        // Inside the resolver's 5-minute buffer, but not yet technically expired — must still
        // trigger a refresh.
        var connection = CreateConnection(organizationId, DateTime.UtcNow.AddMinutes(2));
        var connectionRepository = new FakeConnectionRepository(connection);
        var refreshed = new MercadoPagoOAuthTokenResult(
            AccessToken: "new-access-token",
            RefreshToken: "new-refresh-token",
            SellerUserId: "111",
            ExpiresAt: DateTime.UtcNow.AddDays(180));
        var oauthService = new FakeOAuthService(refreshed);
        var resolver = new MercadoPagoSellerTokenResolver(connectionRepository, oauthService);

        var token = await resolver.ResolveAccessTokenAsync(organizationId, cancellationToken);

        Assert.Equal("new-access-token", token);
        Assert.Equal(1, oauthService.RefreshCallCount);
        Assert.Equal("old-refresh-token", oauthService.LastRefreshTokenPassedIn);

        Assert.Equal(1, connectionRepository.UpsertCallCount);
        var persisted = connectionRepository.LastUpserted!;
        Assert.Equal(connection.Id, persisted.Id);
        Assert.Equal(connection.ConnectedAt, persisted.ConnectedAt);
        Assert.Equal("new-access-token", persisted.AccessToken);
        // The whole point of Mercado Pago's rotation rule: the OLD refresh token must never be
        // the one persisted again.
        Assert.Equal("new-refresh-token", persisted.RefreshToken);
        Assert.NotEqual("old-refresh-token", persisted.RefreshToken);
    }

    [Fact]
    public async Task ResolveAccessTokenAsync_AlreadyExpired_RefreshesAndReturnsNewToken()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var organizationId = Guid.NewGuid();
        var connection = CreateConnection(organizationId, DateTime.UtcNow.AddMinutes(-10));
        var connectionRepository = new FakeConnectionRepository(connection);
        var refreshed = new MercadoPagoOAuthTokenResult(
            AccessToken: "new-access-token",
            RefreshToken: "new-refresh-token",
            SellerUserId: "111",
            ExpiresAt: DateTime.UtcNow.AddDays(180));
        var oauthService = new FakeOAuthService(refreshed);
        var resolver = new MercadoPagoSellerTokenResolver(connectionRepository, oauthService);

        var token = await resolver.ResolveAccessTokenAsync(organizationId, cancellationToken);

        Assert.Equal("new-access-token", token);
        Assert.Equal(1, oauthService.RefreshCallCount);
        Assert.Equal(1, connectionRepository.UpsertCallCount);
    }
}
