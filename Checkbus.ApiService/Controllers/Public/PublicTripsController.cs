using Checkbus.ApiService.Application.Trips.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        [HttpGet("{id:guid}/seatmap")]
        public async Task<IActionResult> GetSeatMap(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetPublicTripSeatMapQuery { TripId = id }, cancellationToken);
            return Ok(result);
        }
    }
}
