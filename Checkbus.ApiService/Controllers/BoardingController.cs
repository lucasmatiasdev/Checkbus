using Checkbus.ApiService.Application.Trips.Commands;
using Checkbus.ApiService.Application.Trips.Queries;
using Checkbus.ApiService.Contracts;
using Checkbus.ApiService.Domain.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class BoardingController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BoardingController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Roles = nameof(Role.Chofer))]
        [HttpGet("my-trips")]
        public async Task<IActionResult> GetMyTrips(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetDriverTripsQuery(), cancellationToken);
            return Ok(result);
        }

        [Authorize(Roles = nameof(Role.Chofer))]
        [HttpGet("{tripId:guid}")]
        public async Task<IActionResult> GetBoardingList(Guid tripId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetTripBoardingListQuery { TripId = tripId }, cancellationToken);
            return Ok(result);
        }

        [Authorize(Roles = nameof(Role.Chofer))]
        [HttpPost("validate")]
        public async Task<IActionResult> Validate([FromBody] ValidateBoardingRequest request, CancellationToken cancellationToken)
        {
            var command = new ValidateBoardingCommand { ReservationId = request.ReservationId, Method = request.Method };
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }

        [Authorize(Roles = nameof(Role.Chofer))]
        [HttpPost("{tripId:guid}/close")]
        public async Task<IActionResult> Close(Guid tripId, CancellationToken cancellationToken)
        {
            await _mediator.Send(new CloseBoardingCommand { TripId = tripId }, cancellationToken);
            return NoContent();
        }

        [Authorize(Roles = nameof(Role.Chofer))]
        [HttpPost("{tripId:guid}/location/toggle")]
        public async Task<IActionResult> ToggleLiveTracking(
            Guid tripId, [FromBody] ToggleTripLiveTrackingRequest request, CancellationToken cancellationToken)
        {
            await _mediator.Send(
                new ToggleTripLiveTrackingCommand { TripId = tripId, Enabled = request.Enabled }, cancellationToken);
            return NoContent();
        }

        [Authorize(Roles = nameof(Role.Chofer))]
        [HttpPost("{tripId:guid}/location")]
        public async Task<IActionResult> UpdateLocation(
            Guid tripId, [FromBody] UpdateTripLocationRequest request, CancellationToken cancellationToken)
        {
            await _mediator.Send(
                new UpdateTripLocationCommand { TripId = tripId, Latitude = request.Latitude, Longitude = request.Longitude },
                cancellationToken);
            return NoContent();
        }
    }
}
