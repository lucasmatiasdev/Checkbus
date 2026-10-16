using System;

namespace Checkbus.ApiService.Domain.Exceptions.Boarding
{
    // Thrown by ValidateBoardingCommandHandler (CU-12) when the Reservation behind the
    // submitted code/row is not Confirmada — a Cancelada reservation must never be markable
    // Abordado, and a PendientePago one hasn't actually been paid for yet either. This checks
    // Reservation.Status directly; BoardingRecord.Status alone (checked separately by
    // IBoardingRecordRepository.ValidateAsync) doesn't know about cancellation.
    public class ReservationNotEligibleForBoardingException : Exception
    {
        public ReservationNotEligibleForBoardingException(string message = "This reservation is not eligible for boarding.") : base(message) { }
    }
}
