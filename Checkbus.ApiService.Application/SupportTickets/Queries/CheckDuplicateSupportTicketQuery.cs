using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.SupportTickets.Queries
{
    // Tenant comes exclusively from the authenticated caller's claim inside the handler, same
    // reasoning as GetMySupportTicketsQuery - no OrganizationId field here.
    public class CheckDuplicateSupportTicketQuery : IRequest<bool>
    {
        public required SupportTicketCategory Category { get; set; }
    }
}
