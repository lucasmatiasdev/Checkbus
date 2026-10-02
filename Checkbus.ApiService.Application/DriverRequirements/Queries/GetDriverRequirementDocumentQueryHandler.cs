using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using MediatR;

namespace Checkbus.ApiService.Application.DriverRequirements.Queries
{
    public class GetDriverRequirementDocumentQueryHandler : IRequestHandler<GetDriverRequirementDocumentQuery, DriverRequirementFileDto>
    {
        private readonly IDriverRequirementRepository _driverRequirementRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUserService _currentUser;

        public GetDriverRequirementDocumentQueryHandler(
            IDriverRequirementRepository driverRequirementRepository,
            IUserRepository userRepository,
            ICurrentUserService currentUser)
        {
            _driverRequirementRepository = driverRequirementRepository;
            _userRepository = userRepository;
            _currentUser = currentUser;
        }

        public async Task<DriverRequirementFileDto> Handle(GetDriverRequirementDocumentQuery request, CancellationToken cancellationToken)
        {
            // Same self-or-admin-same-organization rule as GetDriverRequirementsQuery.
            await GetDriverRequirementsQueryHandler.AuthorizeSelfOrAdminAsync(
                request.TargetUserId, _currentUser, _userRepository, cancellationToken);

            var requirements = await _driverRequirementRepository.GetByUserIdAsync(request.TargetUserId, cancellationToken);
            var requirement = requirements.FirstOrDefault(r => r.Type == request.Type)
                ?? throw new InvalidOperationException(
                    $"DriverRequirement row for user {request.TargetUserId} and type {request.Type} is missing. " +
                    "This row must have been auto-created at Chofer registration time.");

            return new DriverRequirementFileDto
            {
                FileKey = requirement.FileKey,
                FileContentType = requirement.FileContentType
            };
        }
    }
}
