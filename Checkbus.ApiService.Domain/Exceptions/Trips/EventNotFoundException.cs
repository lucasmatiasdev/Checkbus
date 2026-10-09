using System;

namespace Checkbus.ApiService.Domain.Exceptions.Trips
{
    // Event is tenant-scoped (OrganizationId via ITenantEntity) — thrown both when the given
    // EventId does not reference any Event row and when it belongs to another organization, so
    // the response never leaks cross-tenant existence (IDOR-safe, same rationale as every other
    // *NotFoundException in this codebase).
    public class EventNotFoundException : Exception
    {
        public EventNotFoundException(string message = "Event not found.") : base(message) { }
    }
}
