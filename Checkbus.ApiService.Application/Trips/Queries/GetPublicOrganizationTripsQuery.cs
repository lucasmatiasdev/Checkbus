using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    // Public, unauthenticated "viajes disponibles" listing by Organization.Slug
    // (odd/tasks/viajes-disponibles-publico.md T1) — the first public URL to resolve
    // Organization.Slug (every prior public surface resolved Trip.Slug instead, see
    // ResolveTripSlugQuery). DateFrom/DateTo/EventName are all optional further narrowing on top
    // of the handler's own "Publicado and departing in the future" base filter.
    public class GetPublicOrganizationTripsQuery : IRequest<IReadOnlyList<PublicOrganizationTripDto>>
    {
        public required string OrganizationSlug { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? EventName { get; set; }
    }
}
