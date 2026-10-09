using System;

namespace Checkbus.ApiService.Domain.Exceptions.Events
{
    // Event is a shared catalog (no OrganizationId) — no IDOR/cross-tenant rationale needed here,
    // unlike VehicleNotFoundException. Thrown only when the id truly doesn't resolve to a row.
    public class EventNotFoundException : Exception
    {
        public EventNotFoundException(string message = "Event not found.") : base(message) { }
    }
}
