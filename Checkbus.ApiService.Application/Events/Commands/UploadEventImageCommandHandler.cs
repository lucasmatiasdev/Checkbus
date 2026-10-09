using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Interfaces.Storage;
using Checkbus.ApiService.Domain.Exceptions.Events;
using MediatR;

namespace Checkbus.ApiService.Application.Events.Commands
{
    public class UploadEventImageCommandHandler : IRequestHandler<UploadEventImageCommand>
    {
        private readonly IEventRepository _eventRepository;
        private readonly IFileStorageService _fileStorageService;

        public UploadEventImageCommandHandler(IEventRepository eventRepository, IFileStorageService fileStorageService)
        {
            _eventRepository = eventRepository;
            _fileStorageService = fileStorageService;
        }

        public async Task Handle(UploadEventImageCommand request, CancellationToken cancellationToken)
        {
            // Event is a shared catalog (no OrganizationId) — role gate (Planificador/
            // Administrador) is enforced at the controller, same as every other EventsController
            // action. No tenant check here, unlike UploadVehicleDocumentCommandHandler.
            var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
            if (@event is null)
            {
                throw new EventNotFoundException();
            }

            if (@event.ImageKey is not null)
            {
                // Avoid orphaned files on re-upload, same reasoning as
                // UploadVehicleDocumentCommandHandler.
                await _fileStorageService.DeleteAsync(@event.ImageKey, cancellationToken);
            }

            var extension = Path.GetExtension(request.OriginalFileName);
            var key = $"events/{request.EventId}/{Guid.NewGuid()}{extension}";

            await _fileStorageService.SaveAsync(key, request.FileStream, request.ContentType, cancellationToken);

            @event.ImageKey = key;
            @event.ImageContentType = request.ContentType;
            @event.UpdatedAt = DateTime.UtcNow;

            await _eventRepository.UpdateAsync(@event, cancellationToken);
        }
    }
}
