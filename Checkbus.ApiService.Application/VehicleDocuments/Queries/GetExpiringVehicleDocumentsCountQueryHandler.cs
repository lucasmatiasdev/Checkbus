using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using MediatR;

namespace Checkbus.ApiService.Application.VehicleDocuments.Queries
{
    public class GetExpiringVehicleDocumentsCountQueryHandler : IRequestHandler<GetExpiringVehicleDocumentsCountQuery, int>
    {
        private const int ExpiringThresholdDays = 30;

        private readonly IVehicleDocumentRepository _vehicleDocumentRepository;
        private readonly ICurrentUserService _currentUser;

        public GetExpiringVehicleDocumentsCountQueryHandler(
            IVehicleDocumentRepository vehicleDocumentRepository,
            ICurrentUserService currentUser)
        {
            _vehicleDocumentRepository = vehicleDocumentRepository;
            _currentUser = currentUser;
        }

        public async Task<int> Handle(GetExpiringVehicleDocumentsCountQuery request, CancellationToken cancellationToken)
        {
            // Tenant comes exclusively from the validated claim — same anti-IDOR rationale
            // as GetExpiringDriverRequirementsCountQueryHandler.
            var organizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var threshold = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(ExpiringThresholdDays);

            return await _vehicleDocumentRepository.GetExpiringOrExpiredCountByOrganizationAsync(
                organizationId, threshold, cancellationToken);
        }
    }
}
