using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Payments.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.Controllers.Public
{

    [AllowAnonymous]
    [Route("api/public/mercadopago")]
    [ApiController]
    public class MercadoPagoOAuthCallbackController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly PublicWebOptions _publicWebOptions;

        public MercadoPagoOAuthCallbackController(IMediator mediator, PublicWebOptions publicWebOptions)
        {
            _mediator = mediator;
            _publicWebOptions = publicWebOptions;
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback(
            [FromQuery] string? code, [FromQuery] string? state, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
            {

                return Redirect($"{_publicWebOptions.BaseUrl}/payments-settings?connected=false&reason=missing_parameters");
            }

            var result = await _mediator.Send(
                new HandleMercadoPagoOAuthCallbackCommand { Code = code, State = state }, cancellationToken);

            return Redirect(result.RedirectUrl);
        }
    }
}
