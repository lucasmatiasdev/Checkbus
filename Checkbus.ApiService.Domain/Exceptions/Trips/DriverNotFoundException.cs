using System;

namespace Checkbus.ApiService.Domain.Exceptions.Trips
{
    // Deliberately generic: thrown when the target DriverId does not exist, belongs to another
    // organization, or does not have Role.Chofer, so the response never leaks cross-tenant
    // existence or role information (IDOR-safe — every case looks identical to the caller).
    public class DriverNotFoundException : Exception
    {
        public DriverNotFoundException(string message = "Driver not found.") : base(message) { }
    }
}
