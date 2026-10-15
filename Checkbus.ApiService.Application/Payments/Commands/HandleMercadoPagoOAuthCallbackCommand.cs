using MediatR;

namespace Checkbus.ApiService.Application.Payments.Commands
{
    // Anonymous OAuth callback (odd/tasks/pagos-mercadopago.md Fase I, I7) — Code/State come
    // straight off Mercado Pago's own redirect query string, dispatched by
    // MercadoPagoOAuthCallbackController ([AllowAnonymous], see that controller's own doc
    // comment for why). No ICurrentUserService involved anywhere in this path: the caller here
    // is literally a browser Mercado Pago redirected, not an authenticated Checkbus user — the
    // protected State value is the ONLY authorization signal for which Organization this
    // connection belongs to.
    public class HandleMercadoPagoOAuthCallbackCommand : IRequest<MercadoPagoOAuthCallbackResultDto>
    {
        public required string Code { get; set; }
        public required string State { get; set; }
    }
}
