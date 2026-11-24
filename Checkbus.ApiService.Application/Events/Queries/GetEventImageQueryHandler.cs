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

            var callerOrganizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
            if (@event is null || @event.OrganizationId != callerOrganizationId)
            {

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
