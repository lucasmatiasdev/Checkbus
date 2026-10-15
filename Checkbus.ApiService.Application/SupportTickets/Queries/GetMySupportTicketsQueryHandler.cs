using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Support;
using MediatR;

namespace Checkbus.ApiService.Application.SupportTickets.Queries
{
    public class GetMySupportTicketsQueryHandler : IRequestHandler<GetMySupportTicketsQuery, IReadOnlyList<SupportTicketDto>>
    {
        private readonly ISupportTicketRepository _ticketRepository;
        private readonly ICurrentUserService _currentUser;

        public GetMySupportTicketsQueryHandler(
            ISupportTicketRepository ticketRepository,
            ICurrentUserService currentUser)
        {
            _ticketRepository = ticketRepository;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<SupportTicketDto>> Handle(GetMySupportTicketsQuery request, CancellationToken cancellationToken)
        {
            // Tenant comes exclusively from the validated claim - the query has no
            // OrganizationId field, so no request value can ever influence it.
            var organizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var tickets = await _ticketRepository.GetAllByOrganizationAsync(organizationId, cancellationToken);

            return tickets
                .OrderByDescending(t => t.CreatedAt)
                .Select(MapToDto)
                .ToList();
        }

        private static SupportTicketDto MapToDto(SupportTicket ticket)
        {
            return new SupportTicketDto
            {
                Id = ticket.Id,
                Category = ticket.Category,
                Subject = ticket.Subject,
                Description = ticket.Description,
                Status = ticket.Status,
                AttachmentFileName = ticket.AttachmentFileName,
                CreatedAt = ticket.CreatedAt
            };
        }
    }
}
