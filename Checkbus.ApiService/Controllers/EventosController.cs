using Checkbus.ApiService.Application.Eventos.Commands;
using Checkbus.ApiService.Application.Eventos.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.Controllers
{
    // Evento is a shared catalog (no OrganizationId) — every action still requires
    // Planificador or Administrador, matching every other controller's raw-string
    // [Authorize(Roles = "...")] convention (odd/tasks/rutas-publicacion.md Constraint 6).
    [Route("api/[controller]")]
    [ApiController]
    public class EventosController : ControllerBase
    {
        private readonly IMediator _mediator;

        public EventosController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Roles = "Planificador,Administrador")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEventoCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }

        [Authorize(Roles = "Planificador,Administrador")]
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetEventosQuery(), cancellationToken);
            return Ok(result);
        }
    }
}
