using Checkbus.ApiService.Application.Vehicles.Commands;
using Checkbus.ApiService.Application.Vehicles.Queries;
using Checkbus.ApiService.Domain.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VehiclesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public VehiclesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Roles = nameof(Role.Administrador))]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVehicleCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }

        [Authorize(Roles = "Mecanico,Administrador")]
        [HttpGet]
        public async Task<IActionResult> GetVehicles(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetVehiclesQuery(), cancellationToken);
            return Ok(result);
        }
    }
}
