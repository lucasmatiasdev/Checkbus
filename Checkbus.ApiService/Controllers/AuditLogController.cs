using Checkbus.ApiService.Application.Audit.Queries;
using Checkbus.ApiService.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.Controllers
{
    // CU-16 (odd/tasks/auditoria-sistema.md) — visible to Administrador (own organization
    // only) and CheckbusSoporte (every organization, optionally scoped to one via the
    // organizationId filter); the handler re-validates the CheckbusSoporte claim against the
    // sentinel Checkbus Organization, same defense-in-depth idea as SupportTicketsAdminController.
    [Route("api/audit-log")]
    [ApiController]
    public class AuditLogController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuditLogController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Roles = "Administrador,CheckbusSoporte")]
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] AuditAction? action,
            [FromQuery] Guid? actorUserId,
            [FromQuery] Guid? organizationId,
            CancellationToken cancellationToken)
        {
            var query = new GetAuditLogQuery
            {
                DateFrom = dateFrom,
                DateTo = dateTo,
                Action = action,
                ActorUserId = actorUserId,
                OrganizationId = organizationId
            };

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
    }
}
