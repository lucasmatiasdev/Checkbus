using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    // Fase D (odd/tasks/pagos-mercadopago.md) — dispatched by PaymentsWebhookController once
    // D2's signature validation has already passed. Carries only the two ids the controller
    // extracted BEFORE any body parsing: ExternalEventId is the webhook JSON body's top-level
    // "id" field (the notification id, used for D3's dedupe), ExternalOrderId is data.id from
    // the webhook URL's own query string (the Mercado Pago order id). Deliberately nothing else
    // from the webhook payload is carried through — this Fase's own rule is to never trust the
    // webhook body for anything that changes money/seat state; the handler re-fetches
    // authoritative status via IPaymentService.GetOrderAsync instead.
    public class HandlePaymentWebhookCommand : IRequest
    {
        public required string ExternalEventId { get; set; }
        public required string ExternalOrderId { get; set; }
    }
}
