using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.SupportTickets.Queries
{
    public class CheckDuplicateSupportTicketQueryHandler : IRequestHandler<CheckDuplicateSupportTicketQuery, bool>
    {
        // Business decision: a possible duplicate is an open (not Resuelto/Cerrado) ticket in the
        // same category, created by the caller's own organization within this window - a
        // non-blocking signal shown client-side, never a hard validation rule.
        private static readonly TimeSpan DuplicateWindow = TimeSpan.FromHours(24);

        private readonly ISupportTicketRepository _ticketRepository;
        private readonly ICurrentUserService _currentUser;

        public CheckDuplicateSupportTicketQueryHandler(
            ISupportTicketRepository ticketRepository,
            ICurrentUserService currentUser)
        {
            _ticketRepository = ticketRepository;
            _currentUser = currentUser;
        }

        public async Task<bool> Handle(CheckDuplicateSupportTicketQuery request, CancellationToken cancellationToken)
        {
            var organizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var tickets = await _ticketRepository.GetAllByOrganizationAsync(organizationId, cancellationToken);
            var since = DateTime.UtcNow - DuplicateWindow;

            return tickets.Any(t =>
                t.Category == request.Category
                && t.Status != SupportTicketStatus.Resuelto
                && t.Status != SupportTicketStatus.Cerrado
                && t.CreatedAt >= since);
        }
    }
}
