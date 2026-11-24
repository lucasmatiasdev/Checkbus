using System;

namespace Checkbus.ApiService.Domain.Exceptions.Boarding
{

    public class ReservationNotEligibleForBoardingException : Exception
    {
        public ReservationNotEligibleForBoardingException(string message = "This reservation is not eligible for boarding.") : base(message) { }
    }
}
