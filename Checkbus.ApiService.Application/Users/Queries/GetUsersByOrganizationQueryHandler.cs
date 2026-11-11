using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Exceptions.Tenancy;
using MediatR;

namespace Checkbus.ApiService.Application.Users.Queries
{
    public class GetUsersByOrganizationQueryHandler : IRequestHandler<GetUsersByOrganizationQuery, IReadOnlyList<UserListItemDto>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly ICurrentUserService _currentUser;

        public GetUsersByOrganizationQueryHandler(
            IUserRepository userRepository,
            IOrganizationRepository organizationRepository,
            ICurrentUserService currentUser)
        {
            _userRepository = userRepository;
            _organizationRepository = organizationRepository;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<UserListItemDto>> Handle(GetUsersByOrganizationQuery request, CancellationToken cancellationToken)
        {
            // Role gate (CheckbusSoporte) is enforced at the controller; this handler additionally
            // verifies the caller's own OrganizationId resolves to the sentinel Checkbus
            // Organization - defense in depth, same as GetAllSupportTicketsQueryHandler. The
            // request.OrganizationId is the TARGET organization being browsed, never the caller's.
            var callerOrganizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var slug = await _organizationRepository.GetSlugByIdAsync(callerOrganizationId, cancellationToken);
            if (slug != CheckbusInternalOrganization.Slug)
            {
                throw new OrganizationAccessDeniedException();
            }

            var users = await _userRepository.GetAllByOrganizationAsync(request.OrganizationId, cancellationToken);

            return users
                .Select(user => new UserListItemDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Surname = user.Surname,
                    Email = user.Email,
                    Role = user.Role
                })
                .ToList();
        }
    }
}
