using System;

namespace Checkbus.ApiService.Domain.Exceptions.Boarding
{
    // Thrown when a Chofer tries to validate a BoardingRecord that is no longer Pendiente
    // (already Abordado from an earlier validation, or already Ausente from a "Cerrar
    // embarque" close) — re-validating an already-resolved record is rejected clearly rather
    // than silently overwriting ValidatedAt/Method a second time.
    public class BoardingAlreadyValidatedException : Exception
    {
        public BoardingAlreadyValidatedException(string message = "This reservation's boarding record was already resolved.") : base(message) { }
    }
}
