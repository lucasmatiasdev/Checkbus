using MediatR;

namespace Checkbus.ApiService.Application.SupportTickets.Queries
{

    public class GetSupportTicketAttachmentQuery : IRequest<SupportTicketAttachmentDto?>
    {
        public required Guid TicketId { get; set; }
    }
}
