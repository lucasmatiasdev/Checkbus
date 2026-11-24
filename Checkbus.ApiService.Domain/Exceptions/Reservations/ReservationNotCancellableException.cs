using System;

namespace Checkbus.ApiService.Domain.Exceptions.Reservations
{

    public class ReservationNotCancellableException : Exception
    {
        public ReservationNotCancellableException(string message) : base(message) { }
    }
}
