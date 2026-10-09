using Checkbus.ApiService.Application.Trips.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.Controllers.Public
{
    // Sibling of PublicTripsController (odd/tasks/reservas-publicas.md R5), not a third action
    // added to it: this surface is NOT scoped by a single TripId route segment the way
    // GetSeatMap/CreateReservation are — it resolves an arbitrary small batch of Reservation
    // ids (the set returned together by one R4 submission), so it gets its own
    // "api/public/reservations" route instead of nesting under "api/public/trips/{id}/...".
    // Same physical/namespace separation from every authenticated controller as
    // PublicTripsController, and the same R6 caveat: ControllerAuthorizationInventoryTests.cs
    // is EXPECTED to fail once this controller lands, until R6 updates it deliberately.
    [Route("api/public/reservations")]
    [ApiController]
    public class PublicReservationsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PublicReservationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET api/public/reservations?ids=guid1,guid2,guid3 — ASP.NET Core's default query
        // string model binder accepts both repeated keys (?ids=a&ids=b) and one
        // comma-separated value (?ids=a,b) for an IReadOnlyList<Guid> parameter, so either
        // form binds cleanly with no custom model binder.
        //
        // An id that doesn't resolve to a real Reservation is simply omitted by the handler
        // (never an error) — see GetPublicReservationsConfirmationQueryHandler's doc comment.
        // Only when NONE of the submitted ids resolve does this action return 404: that is the
        // one case the task scope calls out explicitly ("if the final list is empty").
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetConfirmation(
            [FromQuery] IReadOnlyList<Guid> ids, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetPublicReservationsConfirmationQuery { ReservationIds = ids ?? [] },
                cancellationToken);

            if (result.Reservations.Count == 0)
            {
                return NotFound();
            }

            return Ok(result);
        }
    }
}
