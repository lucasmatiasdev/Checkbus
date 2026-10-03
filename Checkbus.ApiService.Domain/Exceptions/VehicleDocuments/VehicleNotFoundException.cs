using System;

namespace Checkbus.ApiService.Domain.Exceptions.VehicleDocuments
{
    // Deliberately generic: thrown both when the target vehicle truly does not exist and
    // when it exists in another organization, so the response never leaks cross-tenant
    // existence (IDOR-safe — both cases look identical to the caller). Mirrors
    // DriverRequirementNotFoundException's rationale.
    public class VehicleNotFoundException : Exception
    {
        public VehicleNotFoundException(string message = "Vehicle not found.") : base(message) { }
    }
}
