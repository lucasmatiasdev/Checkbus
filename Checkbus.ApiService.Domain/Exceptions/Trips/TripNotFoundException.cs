using System;

namespace Checkbus.ApiService.Domain.Exceptions.Trips
{
    // Deliberately generic: thrown both when the target Trip truly does not exist and when it
    // exists in another organization, so the response never leaks cross-tenant existence
    // (IDOR-safe — both cases look identical to the caller). Mirrors VehicleNotFoundException's
    // rationale.
    public class TripNotFoundException : Exception
    {
        public TripNotFoundException(string message = "Trip not found.") : base(message) { }
    }
}
