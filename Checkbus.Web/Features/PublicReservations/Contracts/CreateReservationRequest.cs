using Checkbus.Web.Features.Users.Contracts;

namespace Checkbus.Web.Features.PublicReservations.Contracts;

/// <summary>
/// Web-side mirror of the request body for
/// <c>POST /api/public/trips/{id}/reservations</c> (odd/tasks/reservas-publicas.md R4). The
/// route already carries the TripId, so only the per-seat passenger entries are sent.
/// </summary>
public sealed record CreateReservationRequest
{
    public required IReadOnlyList<PassengerSeatRequest> Passengers { get; init; }
}

/// <summary>One passenger's own data for one selected seat, keyed by SeatId.</summary>
public sealed record PassengerSeatRequest
{
    public required Guid SeatId { get; init; }
    public required string Name { get; init; }
    public required string Surname { get; init; }
    public required WebDocumentType DocumentType { get; init; }
    public required string DocumentNumber { get; init; }
    public required string Email { get; init; }
    public required string Phone { get; init; }
}
