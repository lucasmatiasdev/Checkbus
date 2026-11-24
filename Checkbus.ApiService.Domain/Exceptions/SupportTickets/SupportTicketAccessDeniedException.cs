using System;

namespace Checkbus.ApiService.Domain.Exceptions.SupportTickets
{

    public class SupportTicketAccessDeniedException : Exception
    {
        public SupportTicketAccessDeniedException(string message = "Only Checkbus internal staff can perform this action.") : base(message) { }
    }
}
