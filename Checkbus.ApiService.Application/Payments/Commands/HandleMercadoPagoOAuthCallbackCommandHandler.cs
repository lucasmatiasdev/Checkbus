using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Payments;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Payments;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Checkbus.ApiService.Application.Payments.Commands
{
    // odd/tasks/pagos-mercadopago.md Fase I, I7 — the business logic behind the anonymous OAuth
    // callback. Validates/decodes State FIRST (cheap, local, no network call) before ever
    // touching ExchangeCodeAsync — an invalid/expired/tampered State is rejected without
    // spending Mercado Pago's one-time, 10-minute-TTL code on a call whose result would just be
    // discarded anyway.
    public class HandleMercadoPagoOAuthCallbackCommandHandler
        : IRequestHandler<HandleMercadoPagoOAuthCallbackCommand, MercadoPagoOAuthCallbackResultDto>
    {
        private readonly IMercadoPagoOAuthService _oauthService;
        private readonly IMercadoPagoConnectionStateService _stateService;
        private readonly IOrganizationMercadoPagoConnectionRepository _connectionRepository;
        private readonly PublicWebOptions _publicWebOptions;
        private readonly ILogger<HandleMercadoPagoOAuthCallbackCommandHandler> _logger;

        public HandleMercadoPagoOAuthCallbackCommandHandler(
            IMercadoPagoOAuthService oauthService,
            IMercadoPagoConnectionStateService stateService,
            IOrganizationMercadoPagoConnectionRepository connectionRepository,
            PublicWebOptions publicWebOptions,
            ILogger<HandleMercadoPagoOAuthCallbackCommandHandler> logger)
        {
            _oauthService = oauthService;
            _stateService = stateService;
            _connectionRepository = connectionRepository;
            _publicWebOptions = publicWebOptions;
            _logger = logger;
        }

        public async Task<MercadoPagoOAuthCallbackResultDto> Handle(
            HandleMercadoPagoOAuthCallbackCommand request, CancellationToken cancellationToken)
        {
            // Mercado Pago echoes back exactly what BuildAuthorizationUrl sent it as "state":
            // "{organizationId}|{ourProtectedState}" (I3/I4's own confirmed, unchangeable
            // combined shape — see IMercadoPagoConnectionStateService's own doc comment for why
            // the cleartext organizationId prefix here is NEVER trusted on its own). Split on
            // the FIRST '|' only (limit 2) so a protected value that happened to contain one
            // (it won't — Data Protection's own output alphabet has no '|' — but defense in
            // depth costs nothing here) stays intact.
            var parts = request.State.Split('|', 2);
            if (parts.Length != 2)
            {
                _logger.LogWarning("Mercado Pago OAuth callback received a malformed state value.");
                return Failure("invalid_state");
            }

            var stateResult = _stateService.TryUnprotect(parts[1]);
            if (!stateResult.IsValid)
            {
                _logger.LogWarning("Mercado Pago OAuth callback received an invalid or expired state value.");
                return Failure("invalid_state");
            }

            MercadoPagoOAuthTokenResult tokenResult;
            try
            {
                tokenResult = await _oauthService.ExchangeCodeAsync(request.Code, request.State, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(
                    ex,
                    "Mercado Pago OAuth code exchange failed for Organization {OrganizationId}.",
                    stateResult.OrganizationId);
                return Failure("exchange_failed");
            }

            await _connectionRepository.UpsertAsync(new MercadoPagoConnection
            {
                Id = Guid.NewGuid(),
                OrganizationId = stateResult.OrganizationId,
                SellerUserId = tokenResult.SellerUserId,
                AccessToken = tokenResult.AccessToken,
                RefreshToken = tokenResult.RefreshToken,
                ExpiresAt = tokenResult.ExpiresAt,
                ConnectedAt = DateTime.UtcNow
            }, cancellationToken);

            return new MercadoPagoOAuthCallbackResultDto
            {
                Success = true,
                RedirectUrl = BuildRedirectUrl(connected: true, reason: null)
            };
        }

        private MercadoPagoOAuthCallbackResultDto Failure(string reason) => new()
        {
            Success = false,
            FailureReason = reason,
            RedirectUrl = BuildRedirectUrl(connected: false, reason: reason)
        };

        // Same blank-BaseUrl-until-provisioned permissive posture as Fase C5's own back-URLs:
        // a blank BaseUrl produces a relative path rather than failing, which only actually
        // misbehaves (redirects within the API host instead of the Web app) if this callback is
        // genuinely invoked before PublicWebOptions is configured for the environment.
        private string BuildRedirectUrl(bool connected, string? reason)
        {
            var query = connected
                ? "connected=true"
                : $"connected=false&reason={Uri.EscapeDataString(reason ?? "unknown")}";

            return $"{_publicWebOptions.BaseUrl}/payments-settings?{query}";
        }
    }
}
