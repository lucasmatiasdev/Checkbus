using Checkbus.ApiService.Application.VehicleDiagnostics.Commands;
using Checkbus.ApiService.Application.VehicleDiagnostics.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.Controllers
{
    // Every action here requires Mecanico OR Administrador, same posture as
    // MaintenanceRecordsController — no Chofer/Planificador branch exists for this feature
    // at all (odd/tasks/vehicle-maintenance.md Constraint 6).
    [Route("api/[controller]")]
    [ApiController]
    public class VehicleDiagnosticsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public VehicleDiagnosticsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Roles = "Mecanico,Administrador")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVehicleDiagnosticCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }

        [Authorize(Roles = "Mecanico,Administrador")]
        [HttpGet]
        public async Task<IActionResult> GetVehicleDiagnostics([FromQuery] Guid maintenanceRecordId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetVehicleDiagnosticsQuery { MaintenanceRecordId = maintenanceRecordId },
                cancellationToken);
            return Ok(result);
        }
    }
}
