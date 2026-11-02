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
            // Administrador-only is enforced at the controller; this handler only verifies
            // tenant ownership of the target vehicle.
            var callerOrganizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var vehicle = await _vehicleRepository.GetByIdAsync(request.VehicleId, cancellationToken);
            if (vehicle is null || vehicle.OrganizationId != callerOrganizationId)
            {
                // IDOR-safe: cross-tenant existence is never leaked to the caller.
                throw new VehicleNotFoundException();
            }

            var documents = await _vehicleDocumentRepository.GetByVehicleIdAsync(request.VehicleId, cancellationToken);
            // Unlike upload, validate never auto-creates: an admin trying to validate a
            // conditional document type that was never uploaded is a genuine error.
            var document = documents.FirstOrDefault(d => d.Type == request.Type)
                ?? throw new VehicleDocumentNotFoundException();

            // No requirement that DocumentPresent be true first — an admin can mark NoApto
            // on an empty row too (mirrors ValidateDriverRequirementCommandHandler).
            document.Status = request.Approved ? VehicleDocumentStatus.Apto : VehicleDocumentStatus.NoApto;
            document.ValidatedBy = _currentUser.UserId;
            document.UpdatedAt = DateTime.UtcNow;

            await _vehicleDocumentRepository.UpdateAsync(document, cancellationToken);

            // CU-16 (odd/tasks/auditoria-sistema.md S5): no bloqueante — si falla, se loguea y la
            // validación ya persistida sigue su curso normal.
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
