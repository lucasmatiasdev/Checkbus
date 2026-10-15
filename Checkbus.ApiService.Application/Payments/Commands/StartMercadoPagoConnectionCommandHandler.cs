using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Payments;
using MediatR;

namespace Checkbus.ApiService.Application.Payments.Commands
{
    // odd/tasks/pagos-mercadopago.md Fase I, I7 — builds the Mercado Pago authorization URL for
    // the caller's own Organization. No DB write happens here at all: that only happens once the
    // callback (HandleMercadoPagoOAuthCallbackCommandHandler) actually receives a code back.
    public class StartMercadoPagoConnectionCommandHandler
        : IRequestHandler<StartMercadoPagoConnectionCommand, StartMercadoPagoConnectionResultDto>
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IMercadoPagoOAuthService _oauthService;
        private readonly IMercadoPagoConnectionStateService _stateService;

        public StartMercadoPagoConnectionCommandHandler(
            ICurrentUserService currentUser,
            IMercadoPagoOAuthService oauthService,
            IMercadoPagoConnectionStateService stateService)
        {
            _currentUser = currentUser;
            _oauthService = oauthService;
            _stateService = stateService;
        }

        public Task<StartMercadoPagoConnectionResultDto> Handle(
            StartMercadoPagoConnectionCommand request, CancellationToken cancellationToken)
        {
            // Tenant comes exclusively from the validated claim — same convention as
            // GetVehiclesQueryHandler/RegisterUserCommandHandler.
            var organizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var state = _stateService.Protect(organizationId);
            var authorizationUrl = _oauthService.BuildAuthorizationUrl(organizationId, state);

            return Task.FromResult(new StartMercadoPagoConnectionResultDto
            {
                AuthorizationUrl = authorizationUrl
            });
        }
    }
}
