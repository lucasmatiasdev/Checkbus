using Checkbus.ApiService.Application.Users.Commands;
using Checkbus.ApiService.Application.Users.Queries;
using Checkbus.ApiService.Domain.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UsersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Roles = nameof(Role.Administrador))]
        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterUserCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }

        // Planificador needs this for the rutas-publicacion module's "new Trip" form chofer
        // picker (same class of fix as VehiclesController.GetVehicles widening for Mecanico).
        // Register stays Administrador-only.
        [Authorize(Roles = "Administrador,Planificador")]
        [HttpGet]
        public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetUsersQuery(), cancellationToken);
            return Ok(result);
        }

        // CU-16 (odd/tasks/auditoria-sistema.md T4) — [Usuario] dropdown for the audit screen
        // after a CheckbusSoporte caller picks an Organization. Deliberately a separate action/
        // query from GetUsers above (anti-IDOR): the organizationId here is the TARGET being
        // browsed, not the caller's own, so it is CheckbusSoporte-only with its own
        // defense-in-depth re-check in the handler.
        [Authorize(Roles = "CheckbusSoporte")]
        [HttpGet("by-organization/{organizationId:guid}")]
        public async Task<IActionResult> GetUsersByOrganization(Guid organizationId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetUsersByOrganizationQuery { OrganizationId = organizationId }, cancellationToken);
            return Ok(result);
        }
    }
}
