using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Exceptions.SupportTickets;
using MediatR;

namespace Checkbus.ApiService.Application.SupportTickets.Commands
{
    public class UpdateSupportTicketStatusCommandHandler : IRequestHandler<UpdateSupportTicketStatusCommand>
    {
        private readonly ISupportTicketRepository _ticketRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly ICurrentUserService _currentUser;

        public UpdateSupportTicketStatusCommandHandler(
            ISupportTicketRepository ticketRepository,
            IOrganizationRepository organizationRepository,
            ICurrentUserService currentUser)
        {
            _ticketRepository = ticketRepository;
            _organizationRepository = organizationRepository;
            _currentUser = currentUser;
        }

        public async Task Handle(UpdateSupportTicketStatusCommand request, CancellationToken cancellationToken)
        {
            // Role gate (CheckbusSoporte) is enforced at the controller; this handler additionally
            // verifies the caller's own OrganizationId resolves to the sentinel Checkbus
            // Organization - defense in depth, never trust the Role claim alone for this
            // cross-tenant visibility (see CheckbusInternalOrganization's doc comment).
            var organizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var slug = await _organizationRepository.GetSlugByIdAsync(organizationId, cancellationToken);
            if (slug != CheckbusInternalOrganization.Slug)
            {
                throw new SupportTicketAccessDeniedException();
            }

            // Staff legitimately sees every organization's tickets, so a missing TicketId is a
            // genuine 404 - unlike GetSupportTicketAttachmentQueryHandler, there is no cross-tenant
            // existence to hide here.
            var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken)
                ?? throw new SupportTicketNotFoundException();

            ticket.Status = request.NewStatus;
            ticket.UpdatedAt = DateTime.UtcNow;

            await _ticketRepository.UpdateAsync(ticket, cancellationToken);
        }
    }
}
