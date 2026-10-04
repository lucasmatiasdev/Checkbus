using System;

namespace Checkbus.ApiService.Domain.Exceptions.Rutas
{
    // Deliberately generic: thrown when the target ChoferId does not exist, belongs to another
    // organization, or does not have Role.Chofer, so the response never leaks cross-tenant
    // existence or role information (IDOR-safe — every case looks identical to the caller).
    public class ChoferNotFoundException : Exception
    {
        public ChoferNotFoundException(string message = "Chofer not found.") : base(message) { }
    }
}
