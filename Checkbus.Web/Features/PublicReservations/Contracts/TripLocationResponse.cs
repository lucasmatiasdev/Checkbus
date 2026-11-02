namespace Checkbus.Web.Features.PublicReservations.Contracts;

/// <summary>
/// DEMO-ONLY (odd/tasks/seguimiento-tiempo-real-demo.md T4). Web-side mirror of
/// <c>Checkbus.ApiService.Application.Trips.Queries.TripLocationDto</c>, the response body for
/// <c>GET /api/public/trips/{id}/location</c>. Checkbus.Web does not reference the API projects
/// (pure BFF), so this DTO is duplicated here rather than shared — same convention as
/// <see cref="PublicTripDetailResponse"/>. When <see cref="LiveTrackingEnabled"/> is
/// <c>false</c>, every other field is <c>null</c> — there is nothing to show, by design (see the
/// API handler's own doc comment for why "unknown id"/"not in progress"/"tracking off" are
/// deliberately indistinguishable here).
/// </summary>
public sealed record TripLocationResponse
{
    public bool LiveTrackingEnabled { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
