using System;

namespace Checkbus.ApiService.Domain.Exceptions.Boarding
{
    // Thrown by ValidateBoardingCommandHandler/IBoardingRecordRepository.ValidateAsync when the
    // submitted ReservationId does not resolve to any BoardingRecord for the Trip currently
    // being boarded — covers both a garbage/mistyped code and a Reservation that genuinely
    // belongs to a different Trip (mirrors TripNotFoundException's "doesn't exist" framing;
    // this one is never used for a tenant/ownership mismatch, that is TripNotFoundException's
    // job in this slice).
    public class BoardingRecordNotFoundException : Exception
    {
        public BoardingRecordNotFoundException(string message = "Boarding record not found.") : base(message) { }
    }
}
