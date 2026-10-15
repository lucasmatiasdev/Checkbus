using System;

namespace Checkbus.ApiService.Domain.Exceptions.SupportTickets
{
    // Defense in depth for the Checkbus internal staff panel: [Authorize(Roles = "CheckbusSoporte")]
    // on the controller is not proof the caller actually belongs to the sentinel Checkbus
    // Organization (CheckbusInternalOrganization.Slug) - the Role claim alone is never trusted for
    // this cross-tenant visibility, same reasoning as CheckbusInternalOrganization's own doc comment.
    public class SupportTicketAccessDeniedException : Exception
    {
        public SupportTicketAccessDeniedException(string message = "Only Checkbus internal staff can perform this action.") : base(message) { }
    }
}
