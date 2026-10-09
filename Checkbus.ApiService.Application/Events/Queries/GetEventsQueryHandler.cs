using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Trips;
using MediatR;

namespace Checkbus.ApiService.Application.Events.Queries
{
    public class GetEventsQueryHandler : IRequestHandler<GetEventsQuery, IReadOnlyList<EventDto>>
    {
        private readonly IEventRepository _eventRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly ICurrentUserService _currentUser;

        public GetEventsQueryHandler(
            IEventRepository eventRepository,
            ILocationRepository locationRepository,
            ICurrentUserService currentUser)
        {
            _eventRepository = eventRepository;
            _locationRepository = locationRepository;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<EventDto>> Handle(GetEventsQuery request, CancellationToken cancellationToken)
        {
            // Tenant comes exclusively from the validated claim — the query has no
            // OrganizationId field, so no request value can ever influence it.
            var organizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var events = await _eventRepository.GetAllByOrganizationAsync(organizationId, cancellationToken);

            // ILocationRepository has no batch-by-ids lookup, so resolve each distinct
            // Location at most once rather than once per Event — avoids N+1 when multiple
            // events share the same venue, mirroring GetMaintenanceRecordsQueryHandler's
            // single-join-table approach without inventing a new repository method for this.
            var locationsById = new Dictionary<Guid, Location>();
            foreach (var locationId in events.Select(e => e.LocationId).Distinct())
            {
                var location = await _locationRepository.GetByIdAsync(locationId, cancellationToken);
                if (location is not null)
                {
                    locationsById[locationId] = location;
                }
            }

            return events.Select(e => MapToDto(e, locationsById)).ToList();
        }

        private static EventDto MapToDto(Event @event, Dictionary<Guid, Location> locationsById)
        {
            locationsById.TryGetValue(@event.LocationId, out var location);

            return new EventDto
            {
                Id = @event.Id,
                Name = @event.Name,
                Type = @event.Type,
                Date = @event.Date,
                HasImage = @event.ImageKey is not null,
                Location = new EventDto.LocationDto
                {
                    Name = location?.Name ?? string.Empty,
                    Address = location?.Address ?? string.Empty,
                    Latitude = location?.Latitude ?? 0,
                    Longitude = location?.Longitude ?? 0
                }
            };
        }
    }
}
