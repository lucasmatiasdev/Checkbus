using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Interfaces.Storage;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.VehicleDocuments;
using MediatR;

namespace Checkbus.ApiService.Application.VehicleDocuments.Commands
{
    public class UploadVehicleDocumentCommandHandler : IRequestHandler<UploadVehicleDocumentCommand>
    {
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IVehicleDocumentRepository _vehicleDocumentRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly ICurrentUserService _currentUser;

        public UploadVehicleDocumentCommandHandler(
            IVehicleRepository vehicleRepository,
            IVehicleDocumentRepository vehicleDocumentRepository,
            IFileStorageService fileStorageService,
            ICurrentUserService currentUser)
        {
            _vehicleRepository = vehicleRepository;
            _vehicleDocumentRepository = vehicleDocumentRepository;
            _fileStorageService = fileStorageService;
            _currentUser = currentUser;
        }

        public async Task Handle(UploadVehicleDocumentCommand request, CancellationToken cancellationToken)
        {
            // Role gate (Administrador-only) is enforced at the controller via
            // [Authorize(Roles = nameof(Role.Administrador))] — same pattern as
            // CreateVehicleCommandHandler/GetVehiclesQueryHandler. There is no self-service
            // branch for VehicleDocument actions at all, so this handler only needs to
            // verify tenant ownership of the target vehicle.
            var callerOrganizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var vehicle = await _vehicleRepository.GetByIdAsync(request.VehicleId, cancellationToken);
            if (vehicle is null || vehicle.OrganizationId != callerOrganizationId)
            {
                // IDOR-safe: cross-tenant existence is never leaked to the caller.
                throw new VehicleNotFoundException();
            }

            var documents = await _vehicleDocumentRepository.GetByVehicleIdAsync(request.VehicleId, cancellationToken);
            var document = documents.FirstOrDefault(d => d.Type == request.Type);

            var isNewDocument = document is null;
            var now = DateTime.UtcNow;

            if (document is null)
            {
                // Expected, normal case for the 4 conditional types (TituloPropiedad,
                // LeasingInscripto, ContratoAlquiler, HabilitacionEspecifica) on their first
                // upload — their row is never pre-created at vehicle registration time,
                // unlike the 2 universal types.
                document = new VehicleDocument
                {
                    Id = Guid.NewGuid(),
                    VehicleId = request.VehicleId,
                    Type = request.Type,
                    Status = VehicleDocumentStatus.Pendiente,
                    CreatedAt = now,
                    UpdatedAt = now
                };
            }
            else if (document.FileKey is not null)
            {
                // Avoid orphaned files on re-upload.
                await _fileStorageService.DeleteAsync(document.FileKey, cancellationToken);
            }

            var extension = Path.GetExtension(request.OriginalFileName);
            var key = $"vehicle-documents/{request.VehicleId}/{request.Type.ToString().ToLowerInvariant()}/{Guid.NewGuid()}{extension}";

            await _fileStorageService.SaveAsync(key, request.FileStream, request.ContentType, cancellationToken);

            document.FileKey = key;
            document.FileContentType = request.ContentType;
            document.DocumentPresent = true;
            document.IssueDate = request.IssueDate;
            document.ExpirationDate = request.ExpirationDate;
            // Always resets to Pendiente on upload/re-upload — Apto/NoApto stays a
            // deliberate administrator judgment call, never implied by upload alone (an
            // admin might upload an already-expired document and must still review it).
            document.Status = VehicleDocumentStatus.Pendiente;
            document.UpdatedAt = now;

            if (isNewDocument)
            {
                await _vehicleDocumentRepository.AddAsync(document, cancellationToken);
            }
            else
            {
                await _vehicleDocumentRepository.UpdateAsync(document, cancellationToken);
            }
        }
    }
}
