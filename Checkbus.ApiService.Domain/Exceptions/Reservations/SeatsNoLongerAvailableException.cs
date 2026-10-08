using System;

namespace Checkbus.ApiService.Domain.Exceptions.Reservations
{
    // Thrown by CreatePublicReservationCommandHandler (odd/tasks/reservas-publicas.md R4) when
    // one or more requested Seats are no longer free for the target Trip — either caught early
    // by the handler's own pre-check (the common case: someone else's Reservation already
    // exists for that (TripId, SeatId) pair) or, rarely, after a DbUpdateException wrapping a
    // violation of the unique index on Reservation(TripId, SeatId) added in R4 (the real
    // correctness guarantee against the TOCTOU race between page-load and submit). Either path
    // rejects the ENTIRE batch — nothing partial, matching this feature's explicit
    // all-or-nothing design. Distinct from TripNotFoundException (404: the Trip itself doesn't
    // exist/isn't published) — this is a genuine conflict over a specific resource state, so it
    // maps to 409 via ReservationExceptionHandler, not 404 or 400.
    public class SeatsNoLongerAvailableException : Exception
    {
        public SeatsNoLongerAvailableException(
            string message = "One or more selected seats are no longer available for this trip.")
            : base(message)
        {
        }
    }
}
