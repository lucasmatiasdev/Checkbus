using System;

namespace Checkbus.ApiService.Domain.Exceptions.Trips
{
    // A single exception type for every habilitación rule in odd/tasks/rutas-publicacion.md's
    // Scope (vehicle status/documents/overlap, chofer account/requirements/overlap). The message
    // always names which specific rule failed (in Spanish, matching this app's UI-copy
    // convention) and is surfaced directly to the caller via TripExceptionHandler as a 400 — the
    // entity being referenced exists and belongs to the caller's organization, it just fails a
    // business rule, so this is distinct from the 404 *NotFoundException types above.
    public class TripNotEligibleException : Exception
    {
        public TripNotEligibleException(string message) : base(message) { }
    }
}
