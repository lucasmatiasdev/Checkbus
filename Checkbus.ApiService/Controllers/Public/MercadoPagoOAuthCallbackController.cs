using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Payments.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.Controllers.Public
{
    // odd/tasks/pagos-mercadopago.md Fase I, I7 — Mercado Pago's own browser redirect lands
    // here after an admin approves the connection on Mercado Pago's hosted page. Same
    // [AllowAnonymous]/no-ICurrentUserService posture as PublicTripsController/
    // PaymentsWebhookController, for the same underlying reason as both: there is no Checkbus-
    // authenticated caller on this request at all.
    //
    // Specifically NOT the same case as PaymentsWebhookController (Mercado Pago calling an API
    // IN server-to-server) — this is a genuine BROWSER navigation (the admin's own browser,
    // redirected by Mercado Pago after a human clicked "autorizar" on its hosted page). A bearer
    // token is attached by Checkbus.Web's keyed "apiservice" HttpClient (see
    // Checkbus.Web.Features.Users.Services.UsersClient's own doc comment) for an actual API call
    // made FROM a live Blazor circuit — never to a plain top-level browser navigation/redirect
    // like this one, so there is no Authorization header for [Authorize] to validate here even
    // if the admin who started the flow is, in fact, logged in to Checkbus.Web. The protected
    // State value (IMercadoPagoConnectionStateService) is therefore the ONLY authorization
    // signal this action has for which Organization the resulting connection belongs to — see
    // that service's own doc comment for the account-linking attack it defends against.
    //
    // Deliberately a SEPARATE controller from MercadoPagoConnectionsController (not an
    // [AllowAnonymous] action bolted onto that authenticated one): matches this codebase's own
    // established split between its authenticated controller surface
    // (Checkbus.ApiService.Controllers) and its public one (Checkbus.ApiService.Controllers.
    // Public), the same physical/namespace separation PublicTripsController's own doc comment
    // already explains.
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

        // GET, not POST: this is Mercado Pago's own browser redirect
        // ("...&code=...&state=..." on the query string), never a programmatic call Checkbus.Web
        // makes itself.
        [HttpGet("callback")]
        public async Task<IActionResult> Callback(
            [FromQuery] string? code, [FromQuery] string? state, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
            {
                // Mercado Pago's own docs: a denied/cancelled authorization redirects back with
                // an "error" query param instead of code/state at all. Either way there is
                // nothing to exchange — redirect straight to the failure landing rather than
                // dispatching a command that could never succeed.
                return Redirect($"{_publicWebOptions.BaseUrl}/payments-settings?connected=false&reason=missing_parameters");
            }

            var result = await _mediator.Send(
                new HandleMercadoPagoOAuthCallbackCommand { Code = code, State = state }, cancellationToken);

            return Redirect(result.RedirectUrl);
        }
    }
}
