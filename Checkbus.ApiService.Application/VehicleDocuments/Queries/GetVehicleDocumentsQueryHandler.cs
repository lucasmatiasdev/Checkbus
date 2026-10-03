using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Exceptions.VehicleDocuments;
using MediatR;

namespace Checkbus.ApiService.Application.VehicleDocuments.Queries
{
    public class GetVehicleDocumentsQueryHandler : IRequestHandler<GetVehicleDocumentsQuery, IReadOnlyList<VehicleDocumentDto>>
    {
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IVehicleDocumentRepository _vehicleDocumentRepository;
        private readonly ICurrentUserService _currentUser;

        public GetVehicleDocumentsQueryHandler(
            IVehicleRepository vehicleRepository,
            IVehicleDocumentRepository vehicleDocumentRepository,
            ICurrentUserService currentUser)
        {
            _vehicleRepository = vehicleRepository;
            _vehicleDocumentRepository = vehicleDocumentRepository;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<VehicleDocumentDto>> Handle(GetVehicleDocumentsQuery request, CancellationToken cancellationToken)
        {
            var callerOrganizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var vehicle = await _vehicleRepository.GetByIdAsync(request.VehicleId, cancellationToken);
            if (vehicle is null || vehicle.OrganizationId != callerOrganizationId)
            {
                // IDOR-safe: cross-tenant existence is never leaked to the caller.
                throw new VehicleNotFoundException();
            }

            var documents = await _vehicleDocumentRepository.GetByVehicleIdAsync(request.VehicleId, cancellationToken);

            return documents
                .Select(d => new VehicleDocumentDto
                {
                    Type = d.Type,
                    Status = d.Status,
                    IssueDate = d.IssueDate,
                    ExpirationDate = d.ExpirationDate,
                    DocumentPresent = d.DocumentPresent
                })
                .ToList();
        }
    }
}
