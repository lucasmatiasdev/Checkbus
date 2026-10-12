using System;

namespace Checkbus.ApiService.Domain.Exceptions.Reservations
{
    // Thrown by CancelReservationCommandHandler (odd/tasks/reservas-publicas.md Fase 4 / C2)
    // when the submitted Reservation.Id does not resolve to any Reservation at all — same
    // IDOR-safe generic framing as every other *NotFoundException in this codebase
    // (TripNotFoundException, BoardingRecordNotFoundException): there is no authenticated
    // caller on this public surface to distinguish "doesn't exist" from "exists but isn't
    // yours", so this is always the right shape. Maps to 404 via ReservationExceptionHandler.
    public class ReservationNotFoundException : Exception
    {
        public ReservationNotFoundException(string message = "Reservation not found.") : base(message) { }
    }
}
