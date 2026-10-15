using MediatR;

namespace Checkbus.ApiService.Application.SupportTickets.Queries
{
    // Tenant comes exclusively from the authenticated caller's claim inside the handler - no
    // OrganizationId field here, same reasoning as GetEventsQuery/GetVehiclesQuery.
    public class GetMySupportTicketsQuery : IRequest<IReadOnlyList<SupportTicketDto>>
    {
    }
}
