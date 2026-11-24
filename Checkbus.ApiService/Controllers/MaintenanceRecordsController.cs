using Checkbus.ApiService.Application.MaintenanceRecords.Commands;
using Checkbus.ApiService.Application.MaintenanceRecords.Queries;
using Checkbus.ApiService.Contracts;
using Checkbus.ApiService.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.Contracts
{

    public sealed record UpdateMaintenanceRecordRequest(
        MaintenanceStatus Status,
        DateOnly? StartDate,
        DateOnly? EndDate,
        string? MechanicNotes,
        decimal? Cost);
}

namespace Checkbus.ApiService.Controllers
{

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
