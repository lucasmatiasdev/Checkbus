using Checkbus.ApiService.Application.Interfaces.Storage;
using Checkbus.ApiService.Application.Trips.Commands;
using Checkbus.ApiService.Application.Trips.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Checkbus.ApiService.Controllers.Public
{

    [EnableRateLimiting("public-reservations")]
    [Route("api/public/trips")]
    [ApiController]
    public class PublicTripsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PublicTripsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [AllowAnonymous]
        [HttpGet("by-slug/{slug}")]
        public async Task<IActionResult> ResolveSlug(string slug, CancellationToken cancellationToken)
        {
            var tripId = await _mediator.Send(new ResolveTripSlugQuery { Slug = slug }, cancellationToken);
            if (tripId is null)
                return NotFound();

            return Ok(new ResolvedTripResponse { TripId = tripId.Value });
        }

        [AllowAnonymous]
        [HttpGet("{id:guid}/seatmap")]
        public async Task<IActionResult> GetSeatMap(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetPublicTripSeatMapQuery { TripId = id }, cancellationToken);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("{id:guid}/details")]
        public async Task<IActionResult> GetDetails(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetPublicTripDetailQuery { TripId = id }, cancellationToken);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("{id:guid}/location")]
        public async Task<IActionResult> GetLocation(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetTripLocationQuery { TripId = id }, cancellationToken);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("{id:guid}/event-image")]
        public async Task<IActionResult> GetEventImage(
            Guid id,
            [FromServices] IFileStorageService fileStorageService,
            CancellationToken cancellationToken)
        {
            var file = await _mediator.Send(new GetPublicTripEventImageQuery { TripId = id }, cancellationToken);
            if (file.ImageKey is null)
                return NotFound();

            var stream = await fileStorageService.OpenReadAsync(file.ImageKey, cancellationToken);
            if (stream is null)
                return NotFound();

            return File(stream, file.ImageContentType ?? "application/octet-stream");
        }

        [AllowAnonymous]
        [HttpPost("{id:guid}/reservations")]
        public async Task<IActionResult> CreateReservation(
            Guid id, [FromBody] CreatePublicReservationRequestBody body, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new CreatePublicReservationCommand { TripId = id, Passengers = body.Passengers },
                cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }
    }

    public class CreatePublicReservationRequestBody
    {
        public required IReadOnlyList<PassengerSeatRequest> Passengers { get; set; }
    }

    public class ResolvedTripResponse
    {
        public required Guid TripId { get; set; }
    }
}
