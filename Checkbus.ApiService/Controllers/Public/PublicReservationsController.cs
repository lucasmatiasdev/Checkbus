using Checkbus.ApiService.Application.Trips.Commands;
using Checkbus.ApiService.Application.Trips.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Checkbus.ApiService.Controllers.Public
{

    [EnableRateLimiting("public-reservations")]
    [Route("api/public/reservations")]
    [ApiController]
    public class PublicReservationsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PublicReservationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

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

        [AllowAnonymous]
        [HttpPost("{id:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new CancelReservationCommand { ReservationId = id }, cancellationToken);
            return NoContent();
        }
    }
}
