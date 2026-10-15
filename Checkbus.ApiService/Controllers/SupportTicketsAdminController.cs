using Checkbus.ApiService.Application.SupportTickets.Commands;
using Checkbus.ApiService.Application.SupportTickets.Queries;
using Checkbus.ApiService.Contracts;
using Checkbus.ApiService.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.Contracts
{
    // Colocated here rather than its own Contracts file, same convention
    // CreateSupportTicketRequest uses on SupportTicketsController.cs — this task's allowed edit
    // surface only authorizes touching SupportTicketsAdminController.cs, not a new Contracts file.
    public sealed class UpdateSupportTicketStatusRequest
    {
        public required SupportTicketStatus NewStatus { get; set; }
    }
}

namespace Checkbus.ApiService.Controllers
{
    // Checkbus internal staff panel (odd/tasks/panel-soporte-checkbus.md) — cross-tenant ticket
    // management, deliberately on its own route (api/support-tickets-admin) and controller,
    // separate from SupportTicketsController (api/support-tickets, the client-organization
    // audience). Every action requires the CheckbusSoporte role; the handlers additionally
    // verify the caller resolves to the sentinel Checkbus Organization before returning or
    // mutating anything cross-tenant — the Role claim alone is never trusted for that.
    [Route("api/support-tickets-admin")]
    [ApiController]
    public class SupportTicketsAdminController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SupportTicketsAdminController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Roles = "CheckbusSoporte")]
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAllSupportTicketsQuery(), cancellationToken);
            return Ok(result);
        }

        [Authorize(Roles = "CheckbusSoporte")]
        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateSupportTicketStatusRequest request, CancellationToken cancellationToken)
        {
            var command = new UpdateSupportTicketStatusCommand
            {
                TicketId = id,
                NewStatus = request.NewStatus
            };

            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }
    }
}
