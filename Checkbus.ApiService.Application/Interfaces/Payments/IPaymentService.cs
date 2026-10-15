using Checkbus.ApiService.Domain.ValueObjects;

namespace Checkbus.ApiService.Application.Interfaces.Payments
{
    /// <summary>Generic Mercado Pago gateway port. Deliberately NOT a domain-specific method
    /// like <c>ChargeReservationBatch(...)</c> — Infrastructure only knows how to talk to the
    /// Orders API (create/get/refund an order); Application decides what amount, reference and
    /// payer data to send. Same split as
    /// <see cref="Checkbus.ApiService.Application.Interfaces.Email.IEmailService"/>
    /// (odd/tasks/pagos-mercadopago.md Fase B).</summary>
    public interface IPaymentService
    {
        /// <summary>Creates a Mercado Pago Orders API order (<c>type: "online"</c>). Throws
        /// <see cref="Checkbus.ApiService.Domain.Exceptions.Payments.OrderCreationFailedException"/>
        /// if the gateway call itself fails — callers (Fase C) must not swallow this, it is the
        /// signal to roll back the PendientePago batch the order was meant to cover.
        ///
        /// <para><b>Fase I (Marketplace/Split Payments):</b> <paramref name="sellerAccessToken"/>
        /// is the specific Organization's own connected Mercado Pago seller token (resolved via
        /// <c>IMercadoPagoSellerTokenResolver</c>, Fase I6) — real money must land in that
        /// Organization's account, not Checkbus's own. It is declared OPTIONAL/trailing rather
        /// than in the "first param after the request object" position the task originally
        /// suggested: the three existing callers of this port
        /// (<c>CreatePublicReservationCommandHandler</c>, <c>HandlePaymentWebhookCommandHandler</c>,
        /// <c>CancelReservationCommandHandler</c>) are explicitly out of this Fase's edit surface
        /// and all call every <see cref="IPaymentService"/> method positionally with
        /// <c>cancellationToken</c> as the LAST argument — inserting a required/earlier parameter
        /// would have broken their compilation without touching them. A blank default means those
        /// three call sites compile unchanged and continue to behave exactly as before (sending no
        /// access token override) until a later Fase actually wires
        /// <c>IMercadoPagoSellerTokenResolver</c> into them, per I6's own explicit instruction not
        /// to do that wiring in this Fase.</para>
        /// </summary>
        Task<CreateOrderResult> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken, string sellerAccessToken = "");

        /// <summary>Fetches the order's current, authoritative status directly from Mercado
        /// Pago. Fase D's webhook handler must call this instead of trusting the webhook
        /// notification payload's own status field. See <see cref="CreateOrderAsync"/>'s doc
        /// comment for why <paramref name="sellerAccessToken"/> is optional/trailing.</summary>
        Task<OrderStatusResult> GetOrderAsync(string externalOrderId, CancellationToken cancellationToken, string sellerAccessToken = "");

        /// <summary>Refunds an order. <paramref name="amount"/> null means a full refund —
        /// mirrors Mercado Pago's own "empty body = total refund" convention on its refund
        /// endpoint; a non-null amount issues a partial refund for that amount only (Fase F,
        /// one seat of a multi-seat order cancelled). See <see cref="CreateOrderAsync"/>'s doc
        /// comment for why <paramref name="sellerAccessToken"/> is optional/trailing.</summary>
        Task RefundAsync(string externalOrderId, Money? amount, CancellationToken cancellationToken, string sellerAccessToken = "");
    }
}
