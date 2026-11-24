using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Exceptions.DriverRequirements;
using MediatR;

namespace Checkbus.ApiService.Application.DriverRequirements.Queries
{
    public class GetDriverRequirementsQueryHandler : IRequestHandler<GetDriverRequirementsQuery, IReadOnlyList<DriverRequirementDto>>
    {
        private readonly IDriverRequirementRepository _driverRequirementRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUserService _currentUser;

        public GetDriverRequirementsQueryHandler(
            IDriverRequirementRepository driverRequirementRepository,
            IUserRepository userRepository,
            ICurrentUserService currentUser)
        {
            _driverRequirementRepository = driverRequirementRepository;
            _userRepository = userRepository;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<DriverRequirementDto>> Handle(GetDriverRequirementsQuery request, CancellationToken cancellationToken)
        {
            await AuthorizeSelfOrAdminAsync(request.TargetUserId, _currentUser, _userRepository, cancellationToken);

            var requirements = await _driverRequirementRepository.GetByUserIdAsync(request.TargetUserId, cancellationToken);

            return requirements
                .Select(r => new DriverRequirementDto
                {
                    Type = r.Type,
                    Status = r.Status,
                    IssueDate = r.IssueDate,
                    ExpirationDate = r.ExpirationDate,
                    DocumentPresent = r.DocumentPresent
                })
                .ToList();
        }

        internal static async Task AuthorizeSelfOrAdminAsync(
            Guid targetUserId,
            ICurrentUserService currentUser,
            IUserRepository userRepository,
            CancellationToken cancellationToken)
        {
            if (currentUser.Role == nameof(Role.Administrador))
            {
                var callerOrganizationId = currentUser.OrganizationId
                    ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

                var target = await userRepository.FindByIdAsync(targetUserId, cancellationToken);
                if (target is null || target.OrganizationId != callerOrganizationId)
                {
                    throw new DriverRequirementNotFoundException();
                }

                return;
            }

            if (currentUser.UserId != targetUserId)
            {
                throw new DriverRequirementAccessDeniedException();
            }
        }
    }
}
