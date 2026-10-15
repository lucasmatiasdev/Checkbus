using System;

namespace Checkbus.ApiService.Domain.Exceptions.SupportTickets
{
    // Unlike GetSupportTicketAttachmentQueryHandler's IDOR-safe NotFound (which also hides
    // cross-tenant existence from a non-staff caller), this one is thrown exclusively from the
    // Checkbus internal staff panel, where the caller legitimately sees every organization's
    // tickets - a missing TicketId here is a genuine 404, nothing is being hidden.
    public class SupportTicketNotFoundException : Exception
    {
        public SupportTicketNotFoundException(string message = "Support ticket not found.") : base(message) { }
    }
}
