using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Audit;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.SupportTickets;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Checkbus.ApiService.Application.SupportTickets.Commands
{
    public class UpdateSupportTicketStatusCommandHandler : IRequestHandler<UpdateSupportTicketStatusCommand>
    {
        private readonly ISupportTicketRepository _ticketRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ILogger<UpdateSupportTicketStatusCommandHandler> _logger;

        public UpdateSupportTicketStatusCommandHandler(
            ISupportTicketRepository ticketRepository,
            IOrganizationRepository organizationRepository,
            ICurrentUserService currentUser,
            IAuditLogRepository auditLogRepository,
            ILogger<UpdateSupportTicketStatusCommandHandler> logger)
        {
            _ticketRepository = ticketRepository;
            _organizationRepository = organizationRepository;
            _currentUser = currentUser;
            _auditLogRepository = auditLogRepository;
            _logger = logger;
        }

        public async Task Handle(UpdateSupportTicketStatusCommand request, CancellationToken cancellationToken)
        {

            var organizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var slug = await _organizationRepository.GetSlugByIdAsync(organizationId, cancellationToken);
            if (slug != CheckbusInternalOrganization.Slug)
            {
                throw new SupportTicketAccessDeniedException();
            }

            var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken)
                ?? throw new SupportTicketNotFoundException();

            ticket.Status = request.NewStatus;
            ticket.UpdatedAt = DateTime.UtcNow;

            await _ticketRepository.UpdateAsync(ticket, cancellationToken);

            try
            {
                await _auditLogRepository.AddAsync(new AuditLogEntry
                {
                    Id = Guid.NewGuid(),

                    OrganizationId = ticket.OrganizationId,
                    ActorUserId = _currentUser.UserId,
                    ActorEmail = _currentUser.Email ?? string.Empty,
                    Action = AuditAction.EstadoTicketSoporteCambiado,
                    EntityType = "SupportTicket",
                    EntityId = ticket.Id,
                    Description = "Estado de ticket de soporte cambiado",
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write audit log entry for {Action}", AuditAction.EstadoTicketSoporteCambiado);
            }
        }
    }
}
