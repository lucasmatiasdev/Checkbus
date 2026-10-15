using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using MediatR;

namespace Checkbus.ApiService.Application.Payments.Queries
{
    public class GetMercadoPagoConnectionStatusQueryHandler
        : IRequestHandler<GetMercadoPagoConnectionStatusQuery, MercadoPagoConnectionStatusDto>
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IOrganizationMercadoPagoConnectionRepository _connectionRepository;

        public GetMercadoPagoConnectionStatusQueryHandler(
            ICurrentUserService currentUser,
            IOrganizationMercadoPagoConnectionRepository connectionRepository)
        {
            _currentUser = currentUser;
            _connectionRepository = connectionRepository;
        }

        public async Task<MercadoPagoConnectionStatusDto> Handle(
            GetMercadoPagoConnectionStatusQuery request, CancellationToken cancellationToken)
        {
            var organizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var connection = await _connectionRepository.GetByOrganizationIdAsync(organizationId, cancellationToken);

            return new MercadoPagoConnectionStatusDto
            {
                Connected = connection is not null,
                SellerUserId = connection?.SellerUserId
            };
        }
    }
}
