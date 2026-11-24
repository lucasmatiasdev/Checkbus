using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Audit;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.VehicleDocuments;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Checkbus.ApiService.Application.VehicleDocuments.Commands
{
    public class ValidateVehicleDocumentCommandHandler : IRequestHandler<ValidateVehicleDocumentCommand>
    {
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IVehicleDocumentRepository _vehicleDocumentRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ILogger<ValidateVehicleDocumentCommandHandler> _logger;

        public ValidateVehicleDocumentCommandHandler(
            IVehicleRepository vehicleRepository,
            IVehicleDocumentRepository vehicleDocumentRepository,
            ICurrentUserService currentUser,
            IAuditLogRepository auditLogRepository,
            ILogger<ValidateVehicleDocumentCommandHandler> logger)
        {
            _vehicleRepository = vehicleRepository;
            _vehicleDocumentRepository = vehicleDocumentRepository;
            _currentUser = currentUser;
            _auditLogRepository = auditLogRepository;
            _logger = logger;
        }

        public async Task Handle(ValidateVehicleDocumentCommand request, CancellationToken cancellationToken)
        {

            var callerOrganizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var vehicle = await _vehicleRepository.GetByIdAsync(request.VehicleId, cancellationToken);
            if (vehicle is null || vehicle.OrganizationId != callerOrganizationId)
            {

                throw new VehicleNotFoundException();
            }

            var documents = await _vehicleDocumentRepository.GetByVehicleIdAsync(request.VehicleId, cancellationToken);

            var document = documents.FirstOrDefault(d => d.Type == request.Type)
                ?? throw new VehicleDocumentNotFoundException();

            document.Status = request.Approved ? VehicleDocumentStatus.Apto : VehicleDocumentStatus.NoApto;
            document.ValidatedBy = _currentUser.UserId;
            document.UpdatedAt = DateTime.UtcNow;

            await _vehicleDocumentRepository.UpdateAsync(document, cancellationToken);

            try
            {
                await _auditLogRepository.AddAsync(new AuditLogEntry
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = callerOrganizationId,
                    ActorUserId = _currentUser.UserId,
                    ActorEmail = _currentUser.Email ?? string.Empty,
                    Action = AuditAction.DocumentoVehiculoValidado,
                    EntityType = "VehicleDocument",
                    EntityId = document.Id,
                    Description = "Documento de vehículo validado",
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write audit log entry for {Action}", AuditAction.DocumentoVehiculoValidado);
            }
        }
    }
}
