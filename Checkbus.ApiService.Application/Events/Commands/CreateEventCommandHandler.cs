using Checkbus.ApiService.Application.Events.Queries;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Trips;
using MediatR;

namespace Checkbus.ApiService.Application.Events.Commands
{
    public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, EventDto>
    {
        private readonly ILocationRepository _locationRepository;
        private readonly IEventRepository _eventRepository;

        public CreateEventCommandHandler(ILocationRepository locationRepository, IEventRepository eventRepository)
        {
            _locationRepository = locationRepository;
            _eventRepository = eventRepository;
        }

        public async Task<EventDto> Handle(CreateEventCommand request, CancellationToken cancellationToken)
        {
            // Dedupe by PlaceId first (shared Location catalog) — reuse the existing row when
            // the same real-world place was already picked for a previous Event/Trip, otherwise
            // create a new one. Event+Location is not an atomic aggregate (unlike Trip+Route+
            // Stops), so two separate repository calls are fine here per the task's own guidance.
            var location = await _locationRepository.FindByPlaceIdAsync(request.PlaceId, cancellationToken);
            if (location is null)
            {
                var now = DateTime.UtcNow;
                location = new Location
                {
                    Id = Guid.NewGuid(),
                    Name = request.LocationName,
                    Address = request.Address,
                    PlaceId = request.PlaceId,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _locationRepository.AddAsync(location, cancellationToken);
            }

            var createdAt = DateTime.UtcNow;
            var @event = new Event
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Type = request.Type,
                Date = request.Date,
                LocationId = location.Id,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };

            await _eventRepository.AddAsync(@event, cancellationToken);

            return new EventDto
            {
                Id = @event.Id,
                Name = @event.Name,
                Type = @event.Type,
                Date = @event.Date,
                Location = new EventDto.LocationDto
                {
                    Name = location.Name,
                    Address = location.Address,
                    Latitude = location.Latitude,
                    Longitude = location.Longitude
                }
            };
        }
    }
}
