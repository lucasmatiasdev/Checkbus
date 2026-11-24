using System;

namespace Checkbus.ApiService.Domain.Exceptions.Trips
{

    public class TripNotEligibleException : Exception
    {
        public TripNotEligibleException(string message) : base(message) { }
    }
}
