using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Exceptions.Tenancy;
using MediatR;

namespace Checkbus.ApiService.Application.Tenancy.Queries
{
    public class GetOrganizationsQueryHandler : IRequestHandler<GetOrganizationsQuery, IReadOnlyList<OrganizationListItemDto>>
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly ICurrentUserService _currentUser;

        public GetOrganizationsQueryHandler(IOrganizationRepository organizationRepository, ICurrentUserService currentUser)
        {
            _organizationRepository = organizationRepository;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<OrganizationListItemDto>> Handle(GetOrganizationsQuery request, CancellationToken cancellationToken)
        {
            // Role gate (CheckbusSoporte) is enforced at the controller; this handler additionally
            // verifies the caller's own OrganizationId resolves to the sentinel Checkbus
            // Organization - defense in depth, same as GetAllSupportTicketsQueryHandler.
            var organizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var slug = await _organizationRepository.GetSlugByIdAsync(organizationId, cancellationToken);
            if (slug != CheckbusInternalOrganization.Slug)
            {
                throw new OrganizationAccessDeniedException();
            }

            var organizations = await _organizationRepository.GetAllAsync(cancellationToken);

            return organizations
                .Select(o => new OrganizationListItemDto { Id = o.Id, Name = o.Name })
                .ToList();
        }
    }
}
