using Checkbus.ApiService.Application.Payments.Commands;
using Checkbus.ApiService.Application.Payments.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.Controllers
{

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
