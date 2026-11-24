using System;

namespace Checkbus.ApiService.Domain.Exceptions.Reservations
{

    public class ReservationNotFoundException : Exception
    {
        public ReservationNotFoundException(string message = "Reservation not found.") : base(message) { }
    }
}
