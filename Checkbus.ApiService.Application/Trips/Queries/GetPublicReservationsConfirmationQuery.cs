using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    // Public, read-only confirmation screen (odd/tasks/reservas-publicas.md R5). Carries the
    // small set of Reservation ids returned by R4's batch create (a family of 3 booking
    // together gets 3 ids back, shown together on one confirmation page) — kept Trip-rooted
    // under Trips/Queries, same foldering choice R3/R4 made. An id that doesn't resolve to a
    // real Reservation is simply omitted by the handler, never an error; see
    // GetPublicReservationsConfirmationQueryHandler's doc comment for the full access-control
    // rationale.
    public class GetPublicReservationsConfirmationQuery : IRequest<PublicReservationsConfirmationDto>
    {
        public required IReadOnlyList<Guid> ReservationIds { get; set; }
    }
}
