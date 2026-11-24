using Checkbus.ApiService.Application.Trips.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Checkbus.ApiService.Controllers.Public
{

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
