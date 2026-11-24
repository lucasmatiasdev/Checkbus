using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Interfaces.Storage;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using MediatR;

namespace Checkbus.ApiService.Application.Events.Commands
{
    public class UploadEventImageCommandHandler : IRequestHandler<UploadEventImageCommand>
    {
        private readonly IEventRepository _eventRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly ICurrentUserService _currentUser;

        public UploadEventImageCommandHandler(
            IEventRepository eventRepository,
            IFileStorageService fileStorageService,
            ICurrentUserService currentUser)
        {
            _eventRepository = eventRepository;
            _fileStorageService = fileStorageService;
            _currentUser = currentUser;
        }

        public async Task Handle(UploadEventImageCommand request, CancellationToken cancellationToken)
        {

            var callerOrganizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
            if (@event is null || @event.OrganizationId != callerOrganizationId)
            {

                throw new EventNotFoundException();
            }

            if (@event.ImageKey is not null)
            {

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
