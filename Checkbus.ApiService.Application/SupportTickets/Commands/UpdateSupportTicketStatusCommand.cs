using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.SupportTickets.Commands
{
    // Action, not a creation - no return value, same shape as UploadEventImageCommand. Any status
    // to any status is allowed by design (no transition state machine, confirmed scope decision
    // in odd/tasks/panel-soporte-checkbus.md) - do not add transition validation here.
    public class UpdateSupportTicketStatusCommand : IRequest
    {
        public required Guid TicketId { get; set; }
        public required SupportTicketStatus NewStatus { get; set; }
    }
}
