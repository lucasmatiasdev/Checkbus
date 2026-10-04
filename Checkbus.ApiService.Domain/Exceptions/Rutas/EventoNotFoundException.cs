using System;

namespace Checkbus.ApiService.Domain.Exceptions.Rutas
{
    // Evento is a shared catalog (no OrganizationId), so this is a plain existence check, not an
    // IDOR-safe one — thrown when the given EventoId does not reference any Evento row.
    public class EventoNotFoundException : Exception
    {
        public EventoNotFoundException(string message = "Evento not found.") : base(message) { }
    }
}
