using Checkbus.ApiService.Application.Interfaces.Payments;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Payments;
using Checkbus.ApiService.Domain.Exceptions.Payments;

namespace Checkbus.ApiService.Application.Payments
{
    /// <summary>
    /// <see cref="IMercadoPagoSellerTokenResolver"/> implementation (odd/tasks/pagos-mercadopago.md
    /// Fase I, I6). Lives in Application, not Infrastructure — it only orchestrates two existing
    /// Application ports (<see cref="IOrganizationMercadoPagoConnectionRepository"/>,
    /// <see cref="IMercadoPagoOAuthService"/>) with no direct database/HTTP access of its own, the
    /// same reasoning that keeps a MediatR command handler in Application even though it calls
    /// repository/gateway ports whose real implementations live in Infrastructure.
    ///
    /// <para><b>Deliberately NOT wired into any of the three existing <see cref="IPaymentService"/>
    /// callers in this Fase</b> (<c>CreatePublicReservationCommandHandler</c>,
    /// <c>HandlePaymentWebhookCommandHandler</c>, <c>CancelReservationCommandHandler</c>) — per
    /// this Fase's own explicit instruction, that wiring belongs to a later delegation alongside
    /// I7's controller endpoints and I8's Web UI. This class is built, registered in DI, and fully
    /// unit-tested, but nothing calls it yet.</para>
    /// </summary>
    public class MercadoPagoSellerTokenResolver : IMercadoPagoSellerTokenResolver
    {

        public const int ExpirationBufferMinutes = 5;

        private readonly IOrganizationMercadoPagoConnectionRepository _connectionRepository;
        private readonly IMercadoPagoOAuthService _oauthService;

        public MercadoPagoSellerTokenResolver(
            IOrganizationMercadoPagoConnectionRepository connectionRepository,
            IMercadoPagoOAuthService oauthService)
        {
            _connectionRepository = connectionRepository;
            _oauthService = oauthService;
        }

        public async Task<string> ResolveAccessTokenAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            var connection = await _connectionRepository.GetByOrganizationIdAsync(organizationId, cancellationToken)
                ?? throw new OrganizationNotConnectedToMercadoPagoException();

            if (connection.ExpiresAt > DateTime.UtcNow.AddMinutes(ExpirationBufferMinutes))
            {
                return connection.AccessToken;
            }

            var refreshed = await _oauthService.RefreshAsync(connection.RefreshToken, cancellationToken);

            var updatedConnection = new MercadoPagoConnection
            {
                Id = connection.Id,
                OrganizationId = connection.OrganizationId,
                SellerUserId = refreshed.SellerUserId,
                AccessToken = refreshed.AccessToken,
                RefreshToken = refreshed.RefreshToken,
                ExpiresAt = refreshed.ExpiresAt,
                ConnectedAt = connection.ConnectedAt
            };

            await _connectionRepository.UpsertAsync(updatedConnection, cancellationToken);

            return refreshed.AccessToken;
        }
    }
}
