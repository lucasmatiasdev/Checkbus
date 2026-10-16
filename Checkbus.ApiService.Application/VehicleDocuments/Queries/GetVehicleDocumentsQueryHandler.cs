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
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUserService _currentUser;

        public GetVehicleDocumentsQueryHandler(
            IVehicleRepository vehicleRepository,
            IVehicleDocumentRepository vehicleDocumentRepository,
            IUserRepository userRepository,
            ICurrentUserService currentUser)
        {
            _vehicleRepository = vehicleRepository;
            _vehicleDocumentRepository = vehicleDocumentRepository;
            _userRepository = userRepository;
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

            // At most a handful of document types per vehicle, so resolving each distinct
            // validator individually is simpler than adding a batch lookup to IUserRepository.
            var validatorNames = new Dictionary<Guid, string>();
            foreach (var validatorId in documents.Where(d => d.ValidatedBy.HasValue).Select(d => d.ValidatedBy!.Value).Distinct())
            {
                var validator = await _userRepository.FindByIdAsync(validatorId, cancellationToken);
                if (validator is not null)
                {
                    validatorNames[validatorId] = $"{validator.Name} {validator.Surname}";
                }
            }

            return documents
                .Select(d => new VehicleDocumentDto
                {
                    Type = d.Type,
                    Status = d.Status,
                    IssueDate = d.IssueDate,
                    ExpirationDate = d.ExpirationDate,
                    DocumentPresent = d.DocumentPresent,
                    DocumentNumber = d.DocumentNumber,
                    ValidatedByName = d.ValidatedBy.HasValue ? validatorNames.GetValueOrDefault(d.ValidatedBy.Value) : null
                })
                .ToList();
        }
    }
}
