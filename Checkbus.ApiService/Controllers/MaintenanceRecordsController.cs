using Checkbus.ApiService.Application.MaintenanceRecords.Commands;
using Checkbus.ApiService.Application.MaintenanceRecords.Queries;
using Checkbus.ApiService.Contracts;
using Checkbus.ApiService.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.Contracts
{
    // Colocated here rather than its own Contracts file — same convention as
    // ValidateVehicleDocumentRequest in VehicleDocumentsController.cs. The route supplies
    // Id; the body only supplies the lifecycle fields being updated.
    public sealed record UpdateMaintenanceRecordRequest(
        MaintenanceStatus Status,
        DateOnly? StartDate,
        DateOnly? EndDate,
        string? MechanicNotes,
        decimal? Cost);
}

namespace Checkbus.ApiService.Controllers
{
    // Every action here requires Mecanico OR Administrador — unlike VehicleDocumentsController
    // (Administrador-only), this module allows two roles together and has no
    // Chofer/Planificador branch at all (odd/tasks/vehicle-maintenance.md Constraint 6).
    [Route("api/[controller]")]
    [ApiController]
    public class MaintenanceRecordsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public MaintenanceRecordsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Roles = "Mecanico,Administrador")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateMaintenanceRecordCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }

        [Authorize(Roles = "Mecanico,Administrador")]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMaintenanceRecordRequest request, CancellationToken cancellationToken)
        {
            var command = new UpdateMaintenanceRecordCommand
            {
                Id = id,
                Status = request.Status,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                MechanicNotes = request.MechanicNotes,
                Cost = request.Cost
            };

            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }

        [Authorize(Roles = "Mecanico,Administrador")]
        [HttpGet]
        public async Task<IActionResult> GetMaintenanceRecords(
            [FromQuery] Guid? vehicleId,
            [FromQuery] MaintenanceStatus? status,
            [FromQuery] MaintenanceType? type,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetMaintenanceRecordsQuery { VehicleId = vehicleId, Status = status, Type = type },
                cancellationToken);
            return Ok(result);
        }

        [Authorize(Roles = "Mecanico,Administrador")]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetMaintenanceRecord(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMaintenanceRecordQuery { Id = id }, cancellationToken);
            return Ok(result);
        }
    }
}
