using Checkbus.ApiService.Application.Trips.Commands;
using Checkbus.ApiService.Application.Trips.Queries;
using Checkbus.ApiService.Contracts;
using Checkbus.ApiService.Domain.Enums;
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

        // Report for CU-07 (notes/casos-de-uso-adaptacion.md) — separate query/endpoint
        // from GetAll so the existing trips-listing screen stays untouched.
        [Authorize(Roles = "Planificador,Administrador")]
        [HttpGet("report")]
        public async Task<IActionResult> GetReport(
            [FromQuery] DateOnly? dateFrom,
            [FromQuery] DateOnly? dateTo,
            [FromQuery] TripStatus? status,
            [FromQuery] Guid? driverId,
            [FromQuery] Guid? vehicleId,
            CancellationToken cancellationToken)
        {
            var query = new GetTripsReportQuery
            {
                DateFrom = dateFrom,
                DateTo = dateTo,
                Status = status,
                DriverId = driverId,
                VehicleId = vehicleId
            };

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // Export for CU-07 — same filters as GetReport plus an output format, delegates the
        // actual filtering/joins to GetTripsReportQuery via mediator inside the handler.
        [Authorize(Roles = "Planificador,Administrador")]
        [HttpGet("report/export")]
        public async Task<IActionResult> ExportReport(
            [FromQuery] DateOnly? dateFrom,
            [FromQuery] DateOnly? dateTo,
            [FromQuery] TripStatus? status,
            [FromQuery] Guid? driverId,
            [FromQuery] Guid? vehicleId,
            [FromQuery] string format,
            CancellationToken cancellationToken)
        {
            ReportExportFormat parsedFormat;
            switch (format.ToLowerInvariant())
            {
                case "xlsx":
                    parsedFormat = ReportExportFormat.Excel;
                    break;
                case "csv":
                    parsedFormat = ReportExportFormat.Csv;
                    break;
                default:
                    return BadRequest("format must be 'xlsx' or 'csv'.");
            }

            var query = new ExportTripsReportQuery
            {
                DateFrom = dateFrom,
                DateTo = dateTo,
                Status = status,
                DriverId = driverId,
                VehicleId = vehicleId,
                Format = parsedFormat
            };

            var result = await _mediator.Send(query, cancellationToken);
            return File(result.Content, result.ContentType, result.FileName);
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

        // Narrow update of the Route's Stops, not the whole Trip resource — same PUT-with-204
        // shape idea the project doesn't have a prior precedent for on this controller, but
        // matches a plain partial-update convention. TripId comes exclusively from the route
        // ({id}), never trusted from the body (odd/tasks/mejoras-detalle-viaje.md Fase D3).
        [Authorize(Roles = "Planificador,Administrador")]
        [HttpPut("{id:guid}/stops/times")]
        public async Task<IActionResult> UpdateStopTimes(
            Guid id, [FromBody] UpdateStopTimesRequest request, CancellationToken cancellationToken)
        {
            var command = new UpdateStopTimesCommand
            {
                TripId = id,
                Stops = request.Stops
                    .Select(s => new StopDepartureTimeUpdate { StopId = s.StopId, DepartureTime = s.DepartureTime })
                    .ToList()
            };

            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }

        // New authenticated, org-scoped seat-map surface for the Planificador
        // (odd/tasks/mejoras-detalle-viaje.md Fase E1) — same occupancy computation as the
        // public GetPublicTripSeatMapQuery, no Trip.Status == Publicado gate.
        [Authorize(Roles = "Planificador,Administrador")]
        [HttpGet("{id:guid}/seats")]
        public async Task<IActionResult> GetSeatMap(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetTripSeatMapQuery { TripId = id }, cancellationToken);
            return Ok(result);
        }
    }
}
