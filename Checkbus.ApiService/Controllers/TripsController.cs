using Checkbus.ApiService.Application.Trips.Commands;
using Checkbus.ApiService.Application.Trips.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.Controllers
{
    // Every action requires Planificador OR Administrador (odd/tasks/rutas-publicacion.md
    // Constraint 6) — raw comma-separated [Authorize(Roles = "...")], matching every other
    // controller's convention, same as EventsController.
    [Route("api/[controller]")]
    [ApiController]
    public class TripsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TripsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Roles = "Planificador,Administrador")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTripCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }

        [Authorize(Roles = "Planificador,Administrador")]
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetTripsQuery(), cancellationToken);
            return Ok(result);
        }

        [Authorize(Roles = "Planificador,Administrador")]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetTripQuery { Id = id }, cancellationToken);
            return Ok(result);
        }

        // Not a full resource replace (PUT) — a narrow explicit status transition, same REST
        // shape idea as MaintenanceRecordsController.Update but POST-with-204 since this isn't
        // replacing the Trip's fields, only flipping its lifecycle status.
        [Authorize(Roles = "Planificador,Administrador")]
        [HttpPost("{id:guid}/publish")]
        public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new PublishTripCommand { Id = id }, cancellationToken);
            return NoContent();
        }
    }
}
