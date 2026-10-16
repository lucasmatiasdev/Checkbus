using Checkbus.Web.Features.Users.Contracts;

namespace Checkbus.Web.Features.PublicReservations.Contracts;

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.Trips.Queries.PublicReservationsConfirmationDto</c>, the
/// response body for <c>GET /api/public/reservations</c> (odd/tasks/reservas-publicas.md R5).
/// Deliberately excludes Email/Phone, same as the API DTO it mirrors. Reservation.Status is
/// exposed as a plain string (e.g. "Confirmada") rather than a new Web enum mirror + parity
/// test: Fase 1 only ever has one value and this page only displays it, never branches on it.
/// </summary>
public sealed record ReservationsConfirmationResponse
{
    public required IReadOnlyList<ReservationConfirmation> Reservations { get; init; }
}

public sealed record ReservationConfirmation
{
    public required Guid ReservationId { get; init; }
    public int SeatNumber { get; init; }
    public required string PassengerName { get; init; }
    public required string PassengerSurname { get; init; }
    public WebDocumentType PassengerDocumentType { get; init; }
    public required string PassengerDocumentNumber { get; init; }
    public decimal Price { get; init; }
    public required string Status { get; init; }
    public string? EventName { get; init; }
    public DateTime DepartureDate { get; init; }

    // CU-11 replacement ("Consultar estado y progreso del viaje", no GPS hardware).
    public required string TripStatus { get; init; }
    public string? DriverName { get; init; }
    public string? DriverSurname { get; init; }
    public string? VehiclePatent { get; init; }
    public TimeSpan? EstimatedDuration { get; init; }
    public decimal? DistanceKm { get; init; }
    public DateTime? ActualArrivalDate { get; init; }
}
