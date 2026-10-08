namespace Checkbus.Web.Features.PublicReservations.Contracts;

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.Trips.Queries.PublicTripSeatMapDto</c>, the response
/// body for <c>GET /api/public/trips/{id}/seatmap</c> — the project's first unauthenticated
/// business-domain surface (odd/tasks/reservas-publicas.md R3). Checkbus.Web does not
/// reference the API projects (pure BFF), so this DTO is duplicated here rather than shared.
/// Kept under its own <c>PublicReservations</c> feature folder, separate from the
/// authenticated <c>Trips</c> feature, since R4/R5 (reservation create + confirmation) grow
/// this same folder next.
/// </summary>
public sealed record PublicTripSeatMapResponse
{
    public required Guid TripId { get; init; }
    public int Capacity { get; init; }
    public decimal Price { get; init; }
    public required IReadOnlyList<int> OccupiedSeatNumbers { get; init; }
}
