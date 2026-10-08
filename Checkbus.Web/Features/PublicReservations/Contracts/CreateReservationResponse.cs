namespace Checkbus.Web.Features.PublicReservations.Contracts;

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.Trips.Commands.CreatePublicReservationResultDto</c>, the
/// 201 response body for <c>POST /api/public/trips/{id}/reservations</c>
/// (odd/tasks/reservas-publicas.md R4). Just enough for the page's inline success view — the
/// dedicated, persistent confirmation page keyed by these ids is explicitly R5's job, not built
/// here.
/// </summary>
public sealed record CreateReservationResponse
{
    public required IReadOnlyList<CreatedReservation> Reservations { get; init; }
}

public sealed record CreatedReservation
{
    public required Guid ReservationId { get; init; }
    public required int SeatNumber { get; init; }
    public required string PassengerName { get; init; }
    public required string PassengerSurname { get; init; }
    public required decimal Price { get; init; }
}
