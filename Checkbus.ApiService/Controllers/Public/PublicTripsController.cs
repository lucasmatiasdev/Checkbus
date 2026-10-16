using Checkbus.ApiService.Application.Interfaces.Storage;
using Checkbus.ApiService.Application.Trips.Commands;
using Checkbus.ApiService.Application.Trips.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Checkbus.ApiService.Controllers.Public
{
    // This "Public" subfolder/namespace physically separates the project's authenticated
    // controllers (every action under Checkbus.ApiService.Controllers requires a role) from
    // its public, unauthenticated surface. PublicTripsController is the project's first
    // business-domain (non-auth) [AllowAnonymous] controller — see
    // odd/tasks/reservas-publicas.md R3. It deliberately does NOT reuse TripsController:
    // nothing here may assume an authenticated caller or organization scope, which is the
    // opposite assumption every action on TripsController makes.
    //
    // ControllerAuthorizationInventoryTests.cs (owned by R6, not this task) enumerates every
    // controller and anonymous action via reflection against two hardcoded allowlists and is
    // EXPECTED to fail once this controller lands, until R6 updates those lists deliberately —
    // that failure is the intended signal for R6, not a regression to fix here.
    // Rate limiting (odd/tasks/reservas-publicas.md R6): the "public-reservations" policy
    // (Program.cs) is attached here at the controller level, not globally, so it covers every
    // action on this public surface while leaving every authenticated controller untouched.
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

        // Slug->Guid resolution for the friendly share link (CU-03,
        // odd/tasks/gaps-reescritura-cu.md T2). A distinct "by-slug/" prefix keeps this from
        // ever ambiguating with the {id:guid} routes below — those stay untouched.
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

        // Step 1 ("detalle del viaje") data for the public reservation page
        // (odd/tasks/detalle-reserva-publica.md T2) — the third anonymous action on this same
        // public controller, not a new one. Same visibility gate as GetSeatMap.
        [AllowAnonymous]
        [HttpGet("{id:guid}/details")]
        public async Task<IActionResult> GetDetails(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetPublicTripDetailQuery { TripId = id }, cancellationToken);
            return Ok(result);
        }

        // Serves the Step 1 event image anonymously (odd/tasks/detalle-reserva-publica.md T4) —
        // T1's GET api/Events/{id}/image is role-gated, so it can't serve a passenger directly.
        // Resolved through the Trip (not a bare EventId route param) so the same
        // Trip.Status == Publicado gate as every other action here applies — no generic
        // "any Event's image by id" anonymous endpoint is exposed.
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

        // All-or-nothing batch reservation create (odd/tasks/reservas-publicas.md R4). The
        // second anonymous action on this same public controller, not a new one — the natural
        // next step after GetSeatMap on the same public flow. A 409 Conflict (not 404/400) is
        // the correct mapping for "one or more selected seats are no longer available" — see
        // SeatsNoLongerAvailableException/ReservationExceptionHandler.
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

    // The route already carries the TripId ({id:guid}), so the request body only needs the
    // per-seat passenger entries — kept separate from CreatePublicReservationCommand itself so
    // the command's TripId is always the one resolved from the route, never trusted from the
    // body.
    public class CreatePublicReservationRequestBody
    {
        public required IReadOnlyList<PassengerSeatRequest> Passengers { get; set; }
    }

    // Response body of the slug->Guid resolution endpoint above.
    public class ResolvedTripResponse
    {
        public required Guid TripId { get; set; }
    }
}
