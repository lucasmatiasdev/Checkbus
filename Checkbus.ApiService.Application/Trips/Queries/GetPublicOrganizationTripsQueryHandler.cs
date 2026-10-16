using Checkbus.ApiService.Application.Interfaces.Repositories;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    // Deliberately takes NO ICurrentUserService — same public, unauthenticated posture as
    // GetPublicTripSeatMapQueryHandler/GetPublicTripDetailQueryHandler
    // (odd/tasks/viajes-disponibles-publico.md T1). An unknown OrganizationSlug and an
    // organization that exists but currently has no matching trips both resolve to the SAME
    // empty list: an anonymous visitor probing slugs must not be able to distinguish "this
    // organization doesn't exist" from "it exists but has nothing to show right now" by response
    // shape alone (same IDOR-safe rationale as every other public query in this codebase, just
    // expressed as an empty collection instead of a 404 — there is no "the resource addressed by
    // the URL" single entity here, only a listing, so an empty list is the natural equivalent of
    // the seatmap/detail handlers' TripNotFoundException).
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

            // One batch fetch of every Event for the organization rather than one
            // IEventRepository.GetByIdAsync call per Trip — avoids an N+1 read for a listing that
            // can show many trips at once.
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
