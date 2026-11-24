using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.SupportTickets.Queries
{

    public class CheckDuplicateSupportTicketQuery : IRequest<bool>
    {
        public required SupportTicketCategory Category { get; set; }
    }
}
