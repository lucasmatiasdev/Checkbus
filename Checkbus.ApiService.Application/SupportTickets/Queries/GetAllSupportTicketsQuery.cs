using MediatR;

namespace Checkbus.ApiService.Application.SupportTickets.Queries
{
    // Cross-tenant listing for the Checkbus internal staff panel - tenant scoping is deliberately
    // absent here (same reasoning as GetMySupportTicketsQuery has no OrganizationId field for the
    // opposite reason): the caller's identity, not a request value, decides eligibility, and the
    // handler verifies it resolves to the sentinel Checkbus Organization before returning anything.
    public class GetAllSupportTicketsQuery : IRequest<IReadOnlyList<SupportTicketAdminDto>>
    {
    }
}
