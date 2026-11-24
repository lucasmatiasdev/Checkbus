using Checkbus.ApiService.Application.Trips.Commands;
using Checkbus.ApiService.Application.Trips.Queries;
using Checkbus.ApiService.Contracts;
using Checkbus.ApiService.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.Controllers
{

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

        [Authorize(Roles = "Planificador,Administrador")]
        [HttpPost("{id:guid}/publish")]
        public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new PublishTripCommand { Id = id }, cancellationToken);
            return NoContent();
        }

        [Authorize(Roles = "Planificador,Administrador")]
        [HttpPost("{id:guid}/cancel")]
        public async Task<IActionResult> Cancel(
            Guid id, [FromBody] CancelTripRequest request, CancellationToken cancellationToken)
        {
            await _mediator.Send(
                new CancelTripCommand { Id = id, Reason = request.Reason, Description = request.Description },
                cancellationToken);
            return NoContent();
        }

        [Authorize(Roles = "Planificador,Administrador,Chofer")]
        [HttpPost("{id:guid}/start")]
        public async Task<IActionResult> Start(Guid id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new StartTripCommand { Id = id }, cancellationToken);
            return NoContent();
        }

        [Authorize(Roles = "Planificador,Administrador,Chofer")]
        [HttpPost("{id:guid}/finish")]
        public async Task<IActionResult> Finish(
            Guid id, [FromBody] FinishTripRequest request, CancellationToken cancellationToken)
        {
            await _mediator.Send(
                new FinishTripCommand
                {
                    Id = id,
                    ActualArrivalDate = request.ActualArrivalDate,
                    Observations = request.Observations
                },
                cancellationToken);
            return NoContent();
        }

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

        [Authorize(Roles = "Planificador,Administrador")]
        [HttpGet("{id:guid}/pending-refunds")]
        public async Task<IActionResult> GetPendingRefunds(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetTripPendingRefundsQuery { TripId = id }, cancellationToken);
            return Ok(result);
        }

        [Authorize(Roles = "Planificador,Administrador")]
        [HttpGet("{id:guid}/seats")]
        public async Task<IActionResult> GetSeatMap(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetTripSeatMapQuery { TripId = id }, cancellationToken);
            return Ok(result);
        }
    }
}
