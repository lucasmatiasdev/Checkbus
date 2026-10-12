using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    // Public passenger-initiated cancellation (odd/tasks/reservas-publicas.md Fase 4 / C2).
    // No new token/mechanics: Reservation.Id is already the sole access-control mechanism for
    // the whole public confirmation surface (same unguessable-128-bit-GUID reasoning as R5's
    // GetPublicReservationsConfirmationQuery), so this command carries nothing else.
    public class CancelReservationCommand : IRequest
    {
        public required Guid ReservationId { get; set; }
    }
}
