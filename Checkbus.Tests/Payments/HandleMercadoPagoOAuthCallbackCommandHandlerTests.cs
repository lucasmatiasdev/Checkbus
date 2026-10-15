using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Payments;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Payments.Commands;
using Checkbus.ApiService.Domain.Entities.Payments;
using Microsoft.Extensions.Logging.Abstractions;

namespace Checkbus.Tests.Payments;

/// <summary>
/// Proves <see cref="HandleMercadoPagoOAuthCallbackCommandHandler"/> (odd/tasks/pagos-mercadopago.md
/// Fase I, I7): a valid state + successful code exchange upserts the connection and redirects to
/// the success landing; a malformed state (no combined-format separator), an invalid/expired
/// state, and a failed code exchange all redirect to the failure landing WITHOUT ever calling
/// ExchangeCodeAsync/UpsertAsync (whichever is downstream of the failure point) — never throwing
/// into the anonymous callback controller. Fake-service style, no live Mercado Pago credentials.
/// </summary>
public class HandleMercadoPagoOAuthCallbackCommandHandlerTests
{
    private sealed class FakeOAuthService(
        MercadoPagoOAuthTokenResult? exchangeResult = null, Exception? exchangeException = null) : IMercadoPagoOAuthService
    {
        public int ExchangeCallCount { get; private set; }
        public string? ReceivedCode { get; private set; }
        public string? ReceivedState { get; private set; }

        public string BuildAuthorizationUrl(Guid organizationId, string state)
            => throw new NotSupportedException("Not needed by HandleMercadoPagoOAuthCallbackCommandHandler.");

        public Task<MercadoPagoOAuthTokenResult> ExchangeCodeAsync(string code, string state, CancellationToken cancellationToken)
        {
            ExchangeCallCount++;
            ReceivedCode = code;
            ReceivedState = state;

            if (exchangeException is not null)
            {
                throw exchangeException;
            }

            return Task.FromResult(exchangeResult!);
        }

        public Task<MercadoPagoOAuthTokenResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by HandleMercadoPagoOAuthCallbackCommandHandler.");
    }

    private sealed class FakeStateService(bool isValid, Guid organizationId) : IMercadoPagoConnectionStateService
    {
        public int TryUnprotectCallCount { get; private set; }

        public string Protect(Guid organizationId)
            => throw new NotSupportedException("Not needed by HandleMercadoPagoOAuthCallbackCommandHandler.");

        public MercadoPagoConnectionStateResult TryUnprotect(string protectedState)
        {
            TryUnprotectCallCount++;
            return new MercadoPagoConnectionStateResult(isValid, organizationId);
        }
    }

    private sealed class FakeConnectionRepository : IOrganizationMercadoPagoConnectionRepository
    {
        public int UpsertCallCount { get; private set; }
        public MercadoPagoConnection? Upserted { get; private set; }

        public Task<MercadoPagoConnection?> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by HandleMercadoPagoOAuthCallbackCommandHandler.");

        public Task UpsertAsync(MercadoPagoConnection connection, CancellationToken cancellationToken)
        {
            UpsertCallCount++;
            Upserted = connection;
            return Task.CompletedTask;
        }
    }

    private static HandleMercadoPagoOAuthCallbackCommandHandler CreateHandler(
        FakeOAuthService oauthService,
        FakeStateService stateService,
        FakeConnectionRepository connectionRepository,
        PublicWebOptions? publicWebOptions = null) => new(
            oauthService,
            stateService,
            connectionRepository,
            publicWebOptions ?? new PublicWebOptions(),
            NullLogger<HandleMercadoPagoOAuthCallbackCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ValidStateAndSuccessfulExchange_UpsertsConnection_AndRedirectsToSuccess()
    {
        var organizationId = Guid.NewGuid();
        var oauthService = new FakeOAuthService(exchangeResult: new MercadoPagoOAuthTokenResult(
            "access-token", "refresh-token", "seller-123", DateTime.UtcNow.AddDays(180)));
        var stateService = new FakeStateService(isValid: true, organizationId);
        var connectionRepository = new FakeConnectionRepository();
        var handler = CreateHandler(
            oauthService, stateService, connectionRepository,
            new PublicWebOptions { BaseUrl = "https://checkbus.test" });

        var result = await handler.Handle(
            new HandleMercadoPagoOAuthCallbackCommand { Code = "auth-code", State = $"{organizationId}|protected-blob" },
            TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Null(result.FailureReason);
        Assert.Equal("https://checkbus.test/payments-settings?connected=true", result.RedirectUrl);

        Assert.Equal(1, connectionRepository.UpsertCallCount);
        Assert.Equal(organizationId, connectionRepository.Upserted!.OrganizationId);
        Assert.Equal("access-token", connectionRepository.Upserted.AccessToken);
        Assert.Equal("refresh-token", connectionRepository.Upserted.RefreshToken);
        Assert.Equal("seller-123", connectionRepository.Upserted.SellerUserId);

        Assert.Equal("auth-code", oauthService.ReceivedCode);
        // ExchangeCodeAsync's own "state" argument is the WHOLE combined value, per
        // IMercadoPagoOAuthService's own doc comment — never just the protected-blob half.
        Assert.Equal($"{organizationId}|protected-blob", oauthService.ReceivedState);
        Assert.Equal(1, stateService.TryUnprotectCallCount);
    }

    [Fact]
    public async Task Handle_MalformedState_NoSeparator_RedirectsToFailure_WithoutCallingUnprotectExchangeOrUpsert()
    {
        var oauthService = new FakeOAuthService();
        var stateService = new FakeStateService(isValid: true, Guid.NewGuid());
        var connectionRepository = new FakeConnectionRepository();
        var handler = CreateHandler(oauthService, stateService, connectionRepository);

        var result = await handler.Handle(
            new HandleMercadoPagoOAuthCallbackCommand { Code = "auth-code", State = "no-separator-here" },
            TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("invalid_state", result.FailureReason);
        Assert.Contains("connected=false", result.RedirectUrl);
        Assert.Contains("reason=invalid_state", result.RedirectUrl);

        Assert.Equal(0, stateService.TryUnprotectCallCount);
        Assert.Equal(0, oauthService.ExchangeCallCount);
        Assert.Equal(0, connectionRepository.UpsertCallCount);
    }

    [Fact]
    public async Task Handle_TamperedOrExpiredState_RedirectsToFailure_WithoutCallingExchangeOrUpsert()
    {
        var oauthService = new FakeOAuthService();
        // IsValid: false — simulates TryUnprotect rejecting a tampered or expired value.
        var stateService = new FakeStateService(isValid: false, Guid.Empty);
        var connectionRepository = new FakeConnectionRepository();
        var handler = CreateHandler(oauthService, stateService, connectionRepository);

        var result = await handler.Handle(
            new HandleMercadoPagoOAuthCallbackCommand { Code = "auth-code", State = "some-org-id|tampered-or-expired-blob" },
            TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("invalid_state", result.FailureReason);
        Assert.Equal(1, stateService.TryUnprotectCallCount);
        Assert.Equal(0, oauthService.ExchangeCallCount);
        Assert.Equal(0, connectionRepository.UpsertCallCount);
    }

    [Fact]
    public async Task Handle_ExchangeCodeAsyncThrows_RedirectsToFailure_WithoutUpsert()
    {
        var organizationId = Guid.NewGuid();
        var oauthService = new FakeOAuthService(
            exchangeException: new InvalidOperationException("Simulated Mercado Pago OAuth code exchange failure."));
        var stateService = new FakeStateService(isValid: true, organizationId);
        var connectionRepository = new FakeConnectionRepository();
        var handler = CreateHandler(oauthService, stateService, connectionRepository);

        var result = await handler.Handle(
            new HandleMercadoPagoOAuthCallbackCommand { Code = "auth-code", State = $"{organizationId}|protected-blob" },
            TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("exchange_failed", result.FailureReason);
        Assert.Equal(0, connectionRepository.UpsertCallCount);
    }
}
