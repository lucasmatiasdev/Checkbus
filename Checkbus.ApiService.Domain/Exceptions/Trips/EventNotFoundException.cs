using System;

namespace Checkbus.ApiService.Domain.Exceptions.Trips
{
    // Event is a shared catalog (no OrganizationId), so this is a plain existence check, not an
    // IDOR-safe one — thrown when the given EventId does not reference any Event row.
    public class EventNotFoundException : Exception
    {
        public EventNotFoundException(string message = "Event not found.") : base(message) { }
    }
}
