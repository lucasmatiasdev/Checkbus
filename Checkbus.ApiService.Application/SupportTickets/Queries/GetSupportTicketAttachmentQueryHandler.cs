using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using MediatR;

namespace Checkbus.ApiService.Application.SupportTickets.Queries
{
    public class GetSupportTicketAttachmentQueryHandler
        : IRequestHandler<GetSupportTicketAttachmentQuery, SupportTicketAttachmentDto?>
    {
        private readonly ISupportTicketRepository _ticketRepository;
        private readonly ICurrentUserService _currentUser;

        public GetSupportTicketAttachmentQueryHandler(
            ISupportTicketRepository ticketRepository,
            ICurrentUserService currentUser)
        {
            _ticketRepository = ticketRepository;
            _currentUser = currentUser;
        }

        public async Task<SupportTicketAttachmentDto?> Handle(GetSupportTicketAttachmentQuery request, CancellationToken cancellationToken)
        {

            var organizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);

            if (ticket is null || ticket.OrganizationId != organizationId || ticket.AttachmentKey is null)
            {
                return null;
            }

            return new SupportTicketAttachmentDto
            {
                AttachmentKey = ticket.AttachmentKey,
                AttachmentFileName = ticket.AttachmentFileName,
                AttachmentContentType = ticket.AttachmentContentType
            };
        }
    }
}
