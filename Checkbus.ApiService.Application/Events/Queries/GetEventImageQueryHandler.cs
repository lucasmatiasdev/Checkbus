using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using MediatR;

namespace Checkbus.ApiService.Application.Events.Queries
{
    public class GetEventImageQueryHandler : IRequestHandler<GetEventImageQuery, EventImageFileDto>
    {
        private readonly IEventRepository _eventRepository;

        public GetEventImageQueryHandler(IEventRepository eventRepository)
        {
            _eventRepository = eventRepository;
        }

        public async Task<EventImageFileDto> Handle(GetEventImageQuery request, CancellationToken cancellationToken)
        {
            var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken)
                ?? throw new EventNotFoundException();

            return new EventImageFileDto
            {
                ImageKey = @event.ImageKey,
                ImageContentType = @event.ImageContentType
            };
        }
    }
}
