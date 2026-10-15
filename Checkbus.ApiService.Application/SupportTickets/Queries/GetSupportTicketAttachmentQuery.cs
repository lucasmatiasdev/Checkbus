using MediatR;

namespace Checkbus.ApiService.Application.SupportTickets.Queries
{
    // A null result (not an exception) covers both "ticket does not exist", "ticket belongs to
    // another organization" and "ticket has no attachment" - same IDOR-safe shape as
    // GetEventImageQuery/GetPublicTripEventImageQuery's nullable ImageKey.
    public class GetSupportTicketAttachmentQuery : IRequest<SupportTicketAttachmentDto?>
    {
        public required Guid TicketId { get; set; }
    }
}
