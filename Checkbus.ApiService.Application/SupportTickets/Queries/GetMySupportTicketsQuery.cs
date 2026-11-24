using MediatR;

namespace Checkbus.ApiService.Application.SupportTickets.Queries
{

    public class GetMySupportTicketsQuery : IRequest<IReadOnlyList<SupportTicketDto>>
    {
    }
}
