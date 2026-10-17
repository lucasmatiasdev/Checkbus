using Checkbus.ApiService.Application.Notifications.Commands;
using Checkbus.ApiService.Application.Notifications.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.Controllers
{
    // CU-04: standalone notification module. Every action requires Planificador OR
    // Administrador, same raw comma-separated [Authorize(Roles = "...")] convention as
    // TripsController/EventsController — not a side effect of any other controller's action.
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public NotificationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Roles = "Planificador,Administrador")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateNotificationCommand command, CancellationToken cancellationToken)
        {
            await _mediator.Send(command, cancellationToken);
            return StatusCode(StatusCodes.Status201Created);
        }

        [Authorize(Roles = "Planificador,Administrador")]
        [HttpGet]
        public async Task<IActionResult> GetByTrip([FromQuery] Guid tripId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetTripNotificationsQuery { TripId = tripId }, cancellationToken);
            return Ok(result);
        }
    }
}
