using Checkbus.ApiService.Application.SupportTickets.Queries;
using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.SupportTickets.Commands
{

    public class CreateSupportTicketCommand : IRequest<SupportTicketDto>
    {
        public required SupportTicketCategory Category { get; set; }
        public required string Subject { get; set; }
        public required string Description { get; set; }

        public Stream? AttachmentStream { get; set; }
        public string? AttachmentContentType { get; set; }
        public string? AttachmentFileName { get; set; }
    }
}
