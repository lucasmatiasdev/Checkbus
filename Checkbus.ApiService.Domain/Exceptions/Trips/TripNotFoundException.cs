using System;

namespace Checkbus.ApiService.Domain.Exceptions.Trips
{

    public class TripNotFoundException : Exception
    {
        public TripNotFoundException(string message = "Trip not found.") : base(message) { }
    }
}
