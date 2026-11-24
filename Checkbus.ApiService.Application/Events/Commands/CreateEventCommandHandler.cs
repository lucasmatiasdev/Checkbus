using Checkbus.ApiService.Application.Events.Queries;
using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Trips;
using MediatR;

namespace Checkbus.ApiService.Application.Events.Commands
{
    public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, EventDto>
    {
        private readonly ILocationRepository _locationRepository;
        private readonly IEventRepository _eventRepository;
        private readonly ICurrentUserService _currentUser;

        public CreateEventCommandHandler(
            ILocationRepository locationRepository,
            IEventRepository eventRepository,
            ICurrentUserService currentUser)
        {
            _locationRepository = locationRepository;
            _eventRepository = eventRepository;
            _currentUser = currentUser;
        }

        public async Task<EventDto> Handle(CreateEventCommand request, CancellationToken cancellationToken)
        {

            var organizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

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

                Date = DateTime.SpecifyKind(request.Date, DateTimeKind.Utc),
                LocationId = location.Id,
                OrganizationId = organizationId,
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
                HasImage = false,
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
