using Checkbus.ApiService.Application.Trips.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Checkbus.ApiService.Controllers.Public
{
    // Second controller on the project's public, unauthenticated surface (see
    // PublicTripsController's own doc comment for the posture this folder establishes). This one
    // exists for the project's first public URL keyed by Organization.Slug rather than
    // Trip.Slug/Id (odd/tasks/viajes-disponibles-publico.md T1) — a distinct controller, not a
    // new action on PublicTripsController, since the resource addressed here is an Organization,
    // not a Trip.
    //
    // Same rate-limiting rationale as PublicTripsController: this is an anonymous,
    // unauthenticated surface, so it gets the same "public-reservations" policy.
    //
    // NOTE: adding this controller/action changes the known controller and anonymous-action
    // allowlists enumerated by Checkbus.Tests/Auth/ControllerAuthorizationInventoryTests.cs —
    // that file is outside this task's authorized edit surface, so its two allowlists were
    // deliberately left unupdated here (see the task's closing report for the exact lines).
    [EnableRateLimiting("public-reservations")]
    [Route("api/public/organizations")]
    [ApiController]
    public class PublicOrganizationsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PublicOrganizationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // Always 200 OK with a (possibly empty) list — see
        // GetPublicOrganizationTripsQueryHandler's doc comment for why an unknown orgSlug is not
        // distinguished from "exists but nothing to show" via a 404.
        [AllowAnonymous]
        [HttpGet("{orgSlug}/trips")]
        public async Task<IActionResult> GetTrips(
            string orgSlug,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] string? eventName,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetPublicOrganizationTripsQuery
                {
                    OrganizationSlug = orgSlug,
                    DateFrom = dateFrom,
                    DateTo = dateTo,
                    EventName = eventName
                },
                cancellationToken);

            return Ok(result);
        }
    }
}
