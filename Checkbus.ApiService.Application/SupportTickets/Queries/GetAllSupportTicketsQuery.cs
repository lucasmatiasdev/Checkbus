using MediatR;

namespace Checkbus.ApiService.Application.SupportTickets.Queries
{

    public class GetAllSupportTicketsQuery : IRequest<IReadOnlyList<SupportTicketAdminDto>>
    {
    }
}
