using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.SupportTickets.Commands
{

    public class UpdateSupportTicketStatusCommand : IRequest
    {
        public required Guid TicketId { get; set; }
        public required SupportTicketStatus NewStatus { get; set; }
    }
}
