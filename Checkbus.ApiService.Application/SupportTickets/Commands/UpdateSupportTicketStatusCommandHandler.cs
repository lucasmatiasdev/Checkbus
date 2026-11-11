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

            // CU-16 (odd/tasks/auditoria-sistema.md S5): no bloqueante — si falla, se loguea y el
            // cambio de estado ya persistido sigue su curso normal.
            try
            {
                await _auditLogRepository.AddAsync(new AuditLogEntry
                {
                    Id = Guid.NewGuid(),
                    // El ticket pertenece a la organización cliente, distinta de la organización
                    // interna Checkbus del actor (CheckbusSoporte) — así un Administrador de esa
                    // organización puede ver este evento en su propio alcance (CU-16 S1).
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
