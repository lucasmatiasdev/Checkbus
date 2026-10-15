using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Support;
using Checkbus.ApiService.Domain.Exceptions.SupportTickets;
using MediatR;

namespace Checkbus.ApiService.Application.SupportTickets.Queries
{
    public class GetAllSupportTicketsQueryHandler : IRequestHandler<GetAllSupportTicketsQuery, IReadOnlyList<SupportTicketAdminDto>>
    {
        private readonly ISupportTicketRepository _ticketRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly ICurrentUserService _currentUser;

        public GetAllSupportTicketsQueryHandler(
            ISupportTicketRepository ticketRepository,
            IOrganizationRepository organizationRepository,
            ICurrentUserService currentUser)
        {
            _ticketRepository = ticketRepository;
            _organizationRepository = organizationRepository;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<SupportTicketAdminDto>> Handle(GetAllSupportTicketsQuery request, CancellationToken cancellationToken)
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

            var tickets = await _ticketRepository.GetAllAsync(cancellationToken);

            // Resolve each distinct OrganizationId's display Name once, instead of calling the
            // repository once per ticket when several tickets share the same organization.
            var organizationNames = new Dictionary<Guid, string>();
            foreach (var distinctOrganizationId in tickets.Select(t => t.OrganizationId).Distinct())
            {
                var organizationName = await _organizationRepository.GetNameByIdAsync(distinctOrganizationId, cancellationToken);
                organizationNames[distinctOrganizationId] = organizationName ?? "(desconocida)";
            }

            return tickets
                .Select(ticket => MapToDto(ticket, organizationNames[ticket.OrganizationId]))
                .ToList();
        }

        private static SupportTicketAdminDto MapToDto(SupportTicket ticket, string organizationName)
        {
            return new SupportTicketAdminDto
            {
                Id = ticket.Id,
                OrganizationId = ticket.OrganizationId,
                OrganizationName = organizationName,
                Category = ticket.Category,
                Subject = ticket.Subject,
                Description = ticket.Description,
                Priority = ticket.Priority,
                Status = ticket.Status,
                AttachmentFileName = ticket.AttachmentFileName,
                CreatedAt = ticket.CreatedAt
            };
        }
    }
}
