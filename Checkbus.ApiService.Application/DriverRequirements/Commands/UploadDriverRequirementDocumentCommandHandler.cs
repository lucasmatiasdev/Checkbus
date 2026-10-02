using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Interfaces.Storage;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.DriverRequirements;
using MediatR;

namespace Checkbus.ApiService.Application.DriverRequirements.Commands
{
    public class UploadDriverRequirementDocumentCommandHandler : IRequestHandler<UploadDriverRequirementDocumentCommand>
    {
        private readonly IDriverRequirementRepository _driverRequirementRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly ICurrentUserService _currentUser;

        public UploadDriverRequirementDocumentCommandHandler(
            IDriverRequirementRepository driverRequirementRepository,
            IFileStorageService fileStorageService,
            ICurrentUserService currentUser)
        {
            _driverRequirementRepository = driverRequirementRepository;
            _fileStorageService = fileStorageService;
            _currentUser = currentUser;
        }

        public async Task Handle(UploadDriverRequirementDocumentCommand request, CancellationToken cancellationToken)
        {
            // Upload is chofer-self-only: the caller's role must literally be Chofer (an
            // Administrador must never be able to upload "as" a chofer, even for their own
            // id — explicit scope cut), AND the caller must be uploading for their own id.
            if (_currentUser.Role != nameof(Role.Chofer) || _currentUser.UserId != request.TargetUserId)
            {
                throw new DriverRequirementAccessDeniedException();
            }

            var requirements = await _driverRequirementRepository.GetByUserIdAsync(request.TargetUserId, cancellationToken);
            var requirement = requirements.FirstOrDefault(r => r.Type == request.Type)
                ?? throw new InvalidOperationException(
                    $"DriverRequirement row for user {request.TargetUserId} and type {request.Type} is missing. " +
                    "This row must have been auto-created at Chofer registration time.");

            // Avoid orphaned files on re-upload.
            if (requirement.FileKey is not null)
            {
                await _fileStorageService.DeleteAsync(requirement.FileKey, cancellationToken);
            }

            var extension = Path.GetExtension(request.OriginalFileName);
            var key = $"driver-requirements/{request.TargetUserId}/{request.Type.ToString().ToLowerInvariant()}/{Guid.NewGuid()}{extension}";

            await _fileStorageService.SaveAsync(key, request.FileStream, request.ContentType, cancellationToken);

            requirement.FileKey = key;
            requirement.FileContentType = request.ContentType;
            requirement.DocumentPresent = true;
            requirement.IssueDate = request.IssueDate;
            requirement.ExpirationDate = request.ExpirationDate;
            // Always resets to Pendiente on a new upload — a previously-approved document
            // needs re-review once replaced.
            requirement.Status = DriverRequirementStatus.Pendiente;
            requirement.UpdatedAt = DateTime.UtcNow;

            await _driverRequirementRepository.UpdateAsync(requirement, cancellationToken);
        }
    }
}
