using Checkbus.ApiService.Application.Payments.Commands;
using Checkbus.ApiService.Application.Payments.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.Controllers
{
    // odd/tasks/pagos-mercadopago.md Fase I, I7 — authenticated surface for an Organization to
    // connect/inspect its own Mercado Pago seller account. Role gate matches
    // VehiclesController's own convention for a comma-separated multi-role string: this is the
    // same "org-management action" posture as creating a Vehicle, just for payments instead.
    // The anonymous OAuth callback deliberately lives on a SEPARATE controller
    // (MercadoPagoOAuthCallbackController, under Controllers/Public) rather than here — see that
    // controller's own doc comment for why.
    [Route("api/mercadopago-connections")]
    [ApiController]
    public class MercadoPagoConnectionsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public MercadoPagoConnectionsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Roles = "Planificador,Administrador")]
        [HttpPost("start")]
        public async Task<IActionResult> Start(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new StartMercadoPagoConnectionCommand(), cancellationToken);
            return Ok(result);
        }

        [Authorize(Roles = "Planificador,Administrador")]
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMercadoPagoConnectionStatusQuery(), cancellationToken);
            return Ok(result);
        }
    }
}
