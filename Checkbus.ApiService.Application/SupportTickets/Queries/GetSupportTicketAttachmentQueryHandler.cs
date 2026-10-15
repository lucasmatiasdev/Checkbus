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
            // Tenant comes exclusively from the validated claim - the query has no
            // OrganizationId field, so no request value can ever influence it.
            var organizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);

            // IDOR-safe: cross-tenant existence is never leaked to the caller, same posture as
            // UploadVehicleDocumentCommandHandler/UploadEventImageCommandHandler - a missing
            // ticket, a ticket owned by another organization, and a ticket with no attachment
            // all collapse into the same null result.
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
