using System;

namespace Checkbus.ApiService.Domain.Exceptions.Rutas
{
    // Deliberately generic: thrown both when the target Viaje truly does not exist and when it
    // exists in another organization, so the response never leaks cross-tenant existence
    // (IDOR-safe — both cases look identical to the caller). Mirrors VehicleNotFoundException's
    // rationale.
    public class ViajeNotFoundException : Exception
    {
        public ViajeNotFoundException(string message = "Viaje not found.") : base(message) { }
    }
}
