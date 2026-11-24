using Checkbus.ApiService.Application.Interfaces.Repositories;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{

    public class GetPublicOrganizationTripsQueryHandler
        : IRequestHandler<GetPublicOrganizationTripsQuery, IReadOnlyList<PublicOrganizationTripDto>>
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly ITripRepository _tripRepository;
        private readonly IEventRepository _eventRepository;

        public GetPublicOrganizationTripsQueryHandler(
            IOrganizationRepository organizationRepository,
            ITripRepository tripRepository,
            IEventRepository eventRepository)
        {
            _organizationRepository = organizationRepository;
            _tripRepository = tripRepository;
            _eventRepository = eventRepository;
        }

        public async Task<IReadOnlyList<PublicOrganizationTripDto>> Handle(
            GetPublicOrganizationTripsQuery request, CancellationToken cancellationToken)
        {
            var organizationId = await _organizationRepository.GetIdBySlugAsync(
                request.OrganizationSlug, cancellationToken);

            if (organizationId is null)
            {
                return [];
            }

            var trips = await _tripRepository.GetPublicUpcomingByOrganizationIdAsync(
                organizationId.Value, request.DateFrom, request.DateTo, request.EventName, cancellationToken);

            if (trips.Count == 0)
            {
                return [];
            }

            var events = await _eventRepository.GetAllByOrganizationAsync(organizationId.Value, cancellationToken);
            var eventNamesById = events.ToDictionary(e => e.Id, e => e.Name);

            return trips
                .Select(t => new PublicOrganizationTripDto
                {
                    TripSlug = t.Slug,
                    EventName = eventNamesById.TryGetValue(t.EventId, out var name) ? name : string.Empty,
                    DepartureDate = t.DepartureDate,
                    Price = t.Price
                })
                .ToList();
        }
    }
}
