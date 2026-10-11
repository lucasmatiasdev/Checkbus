using System;

namespace Checkbus.ApiService.Domain.Exceptions.Reservations
{
    // Thrown by CancelReservationCommandHandler (odd/tasks/reservas-publicas.md Fase 4 / C2)
    // for either of the two independent cancellation-blocking reasons — either one alone is
    // enough to reject: the Reservation's BoardingRecord is already Abordado, or fewer than
    // CancelReservationCommandHandler.CancellationWindowHours remain before Trip.DepartureDate.
    // One type, two call sites, same spirit as BoardingAlreadyValidatedException's
    // single-reason-one-type shape — but unlike that one, the message IS the user-facing
    // Spanish text shown verbatim near the reservation row on the confirmation page (via
    // ReservationExceptionHandler's 409 ProblemDetails.Detail -> CancelReservationOutcome.
    // NotCancellable), so every call site must supply its own contextual message rather than
    // relying on a generic default.
    public class ReservationNotCancellableException : Exception
    {
        public ReservationNotCancellableException(string message) : base(message) { }
    }
}
