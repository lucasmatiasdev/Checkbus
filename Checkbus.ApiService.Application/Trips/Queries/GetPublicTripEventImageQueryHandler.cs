using Checkbus.ApiService.Application.Events.Queries;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    // No ICurrentUserService — same public, unauthenticated posture as every other handler on
    // this surface. Same TripNotFoundException for "doesn't exist" and "exists but not
    // Publicado" as GetPublicTripDetailQueryHandler/GetPublicTripSeatMapQueryHandler.
    public class GetPublicTripEventImageQueryHandler : IRequestHandler<GetPublicTripEventImageQuery, EventImageFileDto>
    {
        private readonly ITripRepository _tripRepository;
        private readonly IEventRepository _eventRepository;

        public GetPublicTripEventImageQueryHandler(ITripRepository tripRepository, IEventRepository eventRepository)
        {
            _tripRepository = tripRepository;
            _eventRepository = eventRepository;
        }

        public async Task<EventImageFileDto> Handle(GetPublicTripEventImageQuery request, CancellationToken cancellationToken)
        {
            var trip = await _tripRepository.GetByIdAsync(request.TripId, cancellationToken);
            if (trip is null || trip.Status != TripStatus.Publicado)
            {
                throw new TripNotFoundException();
            }

            var @event = await _eventRepository.GetByIdAsync(trip.EventId, cancellationToken);

            return new EventImageFileDto
            {
                ImageKey = @event?.ImageKey,
                ImageContentType = @event?.ImageContentType
            };
        }
    }
}
