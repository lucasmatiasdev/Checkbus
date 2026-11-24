using System;

namespace Checkbus.ApiService.Domain.Exceptions.Reservations
{

    public class SeatsNoLongerAvailableException : Exception
    {
        public SeatsNoLongerAvailableException(
            string message = "One or more selected seats are no longer available for this trip.")
            : base(message)
        {
        }
    }
}
