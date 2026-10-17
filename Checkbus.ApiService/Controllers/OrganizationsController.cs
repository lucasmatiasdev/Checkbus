using Checkbus.ApiService.Application.Tenancy.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.Controllers
{
    // Internal CheckbusSoporte tool (odd/tasks/alta-organizacion-soporte.md) — creates a brand
    // new Organization together with its first Administrador User in one step. Deliberately not
    // public self-service: every action requires the CheckbusSoporte role, same mechanism
    // SupportTicketsAdminController already uses for its own internal-staff-only surface.
    [Route("api/[controller]")]
    [ApiController]
    public class OrganizationsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public OrganizationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Roles = "CheckbusSoporte")]
        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterOrganizationCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }
    }
}
