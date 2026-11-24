using System;

namespace Checkbus.ApiService.Domain.Exceptions.Trips
{

    public class DriverNotFoundException : Exception
    {
        public DriverNotFoundException(string message = "Driver not found.") : base(message) { }
    }
}
