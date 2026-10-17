namespace Checkbus.Web.Features.PublicReservations.Contracts;

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.Trips.Queries.PublicOrganizationTripDto</c>, one item of
/// the response body for <c>GET /api/public/organizations/{orgSlug}/trips</c>
/// (odd/tasks/viajes-disponibles-publico.md T2). Checkbus.Web does not reference the API
/// projects (pure BFF), so this DTO is duplicated here rather than shared — same convention as
/// <see cref="PublicTripDetailResponse"/>. Deliberately thinner than that type: this is a
/// listing card, not the trip detail step, so there is no TripId/driver/vehicle here — only
/// what the card shows, plus TripSlug to link into the existing /reservar/{slug} flow.
/// </summary>
public sealed record PublicOrganizationTripResponse
{
    public required string TripSlug { get; init; }
    public required string EventName { get; init; }
    public DateTime DepartureDate { get; init; }
    public decimal Price { get; init; }
}
