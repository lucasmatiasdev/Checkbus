using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using MediatR;

namespace Checkbus.ApiService.Application.Users.Queries
{
    public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, IReadOnlyList<UserListItemDto>>
    {
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUserService _currentUser;

        public GetUsersQueryHandler(IUserRepository userRepository, ICurrentUserService currentUser)
        {
            _userRepository = userRepository;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<UserListItemDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            // Tenant comes exclusively from the validated claim — the query has no
            // OrganizationId field, so no request value can ever influence it.
            var organizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var users = await _userRepository.GetAllByOrganizationAsync(organizationId, cancellationToken);

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
