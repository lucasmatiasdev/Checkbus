using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using MediatR;

namespace Checkbus.ApiService.Application.DriverRequirements.Queries
{
    public class GetExpiringDriverRequirementsCountQueryHandler : IRequestHandler<GetExpiringDriverRequirementsCountQuery, int>
    {
        private const int ExpiringThresholdDays = 30;

        private readonly IDriverRequirementRepository _driverRequirementRepository;
        private readonly ICurrentUserService _currentUser;

        public GetExpiringDriverRequirementsCountQueryHandler(
            IDriverRequirementRepository driverRequirementRepository,
            ICurrentUserService currentUser)
        {
            _driverRequirementRepository = driverRequirementRepository;
            _currentUser = currentUser;
        }

        public async Task<int> Handle(GetExpiringDriverRequirementsCountQuery request, CancellationToken cancellationToken)
        {
            // Tenant comes exclusively from the validated claim — same anti-IDOR rationale
            // as GetUsersQueryHandler.
            var organizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var threshold = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(ExpiringThresholdDays);

            return await _driverRequirementRepository.GetExpiringOrExpiredCountByOrganizationAsync(
                organizationId, threshold, cancellationToken);
        }
    }
}
