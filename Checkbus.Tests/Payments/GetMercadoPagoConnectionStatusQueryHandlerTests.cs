using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Payments.Queries;
using Checkbus.ApiService.Domain.Entities.Payments;

namespace Checkbus.Tests.Payments;

/// <summary>
/// Proves <see cref="GetMercadoPagoConnectionStatusQueryHandler"/> (odd/tasks/pagos-mercadopago.md
/// Fase I, I8): no connection -> Connected=false/SellerUserId=null; an existing connection ->
/// Connected=true with its SellerUserId; a missing OrganizationId claim throws — same anti-IDOR
/// convention as GetVehiclesQueryHandlerTests.
/// </summary>
public class GetMercadoPagoConnectionStatusQueryHandlerTests
{
    private sealed class FakeCurrentUserService(Guid? organizationId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.NewGuid();
        public Guid? OrganizationId => organizationId;
        public string? Role => "Administrador";
        public string? Email => "admin@checkbus.test";
    }

    private sealed class FakeConnectionRepository(MercadoPagoConnection? connection) : IOrganizationMercadoPagoConnectionRepository
    {
        public Task<MercadoPagoConnection?> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken)
            => Task.FromResult(connection is not null && connection.OrganizationId == organizationId ? connection : null);

        public Task UpsertAsync(MercadoPagoConnection connection, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetMercadoPagoConnectionStatusQueryHandler.");
    }

    private static MercadoPagoConnection CreateConnection(Guid organizationId) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = organizationId,
        SellerUserId = "seller-123",
        AccessToken = "access-token",
        RefreshToken = "refresh-token",
        ExpiresAt = DateTime.UtcNow.AddDays(180),
        ConnectedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task Handle_NoConnection_ReturnsNotConnectedWithNullSellerUserId()
    {
        var organizationId = Guid.NewGuid();
        var handler = new GetMercadoPagoConnectionStatusQueryHandler(
            new FakeCurrentUserService(organizationId), new FakeConnectionRepository(null));

        var result = await handler.Handle(new GetMercadoPagoConnectionStatusQuery(), TestContext.Current.CancellationToken);

        Assert.False(result.Connected);
        Assert.Null(result.SellerUserId);
    }

    [Fact]
    public async Task Handle_HasConnection_ReturnsConnectedWithSellerUserId()
    {
        var organizationId = Guid.NewGuid();
        var connection = CreateConnection(organizationId);
        var handler = new GetMercadoPagoConnectionStatusQueryHandler(
            new FakeCurrentUserService(organizationId), new FakeConnectionRepository(connection));

        var result = await handler.Handle(new GetMercadoPagoConnectionStatusQuery(), TestContext.Current.CancellationToken);

        Assert.True(result.Connected);
        Assert.Equal("seller-123", result.SellerUserId);
    }

    [Fact]
    public async Task Handle_NoOrganizationIdClaim_ThrowsInvalidOperation()
    {
        var handler = new GetMercadoPagoConnectionStatusQueryHandler(
            new FakeCurrentUserService(organizationId: null), new FakeConnectionRepository(null));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new GetMercadoPagoConnectionStatusQuery(), TestContext.Current.CancellationToken));
    }
}
