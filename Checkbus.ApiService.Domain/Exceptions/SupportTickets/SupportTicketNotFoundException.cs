using System;

namespace Checkbus.ApiService.Domain.Exceptions.SupportTickets
{

    public class SupportTicketNotFoundException : Exception
    {
        public SupportTicketNotFoundException(string message = "Support ticket not found.") : base(message) { }
    }
}
