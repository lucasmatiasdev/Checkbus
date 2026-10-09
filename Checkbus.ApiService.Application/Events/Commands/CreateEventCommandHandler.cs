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
            // Tenant comes exclusively from the validated claim — the command has no
            // OrganizationId field, so no request body value can ever influence it.
            var organizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

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
                // Checkbus.Web's MudDatePicker produces a Kind=Unspecified DateTime (it survives
                // the JSON round-trip as-is, with no offset); Npgsql's "timestamp with time zone"
                // column only accepts Utc. The picked date has no real time-of-day/timezone
                // meaning, so it's tagged Utc rather than converted.
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
                HasImage = false, // a freshly created Event never has an image yet
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
