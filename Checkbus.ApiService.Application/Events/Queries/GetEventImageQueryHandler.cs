using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using MediatR;

namespace Checkbus.ApiService.Application.Events.Queries
{
    public class GetEventImageQueryHandler : IRequestHandler<GetEventImageQuery, EventImageFileDto>
    {
        private readonly IEventRepository _eventRepository;
        private readonly ICurrentUserService _currentUser;

        public GetEventImageQueryHandler(IEventRepository eventRepository, ICurrentUserService currentUser)
        {
            _eventRepository = eventRepository;
            _currentUser = currentUser;
        }

        public async Task<EventImageFileDto> Handle(GetEventImageQuery request, CancellationToken cancellationToken)
        {
            // Role gate (Planificador/Administrador) is enforced at the controller; this handler
            // only needs to verify tenant ownership of the target Event, same pattern as
            // GetVehicleDocumentQueryHandler.
            var callerOrganizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
            if (@event is null || @event.OrganizationId != callerOrganizationId)
            {
                // IDOR-safe: cross-tenant existence is never leaked to the caller.
                throw new EventNotFoundException();
            }

            return new EventImageFileDto
            {
                ImageKey = @event.ImageKey,
                ImageContentType = @event.ImageContentType
            };
        }
    }
}
