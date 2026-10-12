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
        /// signal to roll back the PendientePago batch the order was meant to cover.</summary>
        Task<CreateOrderResult> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken);

        /// <summary>Fetches the order's current, authoritative status directly from Mercado
        /// Pago. Fase D's webhook handler must call this instead of trusting the webhook
        /// notification payload's own status field.</summary>
        Task<OrderStatusResult> GetOrderAsync(string externalOrderId, CancellationToken cancellationToken);

        /// <summary>Refunds an order. <paramref name="amount"/> null means a full refund —
        /// mirrors Mercado Pago's own "empty body = total refund" convention on its refund
        /// endpoint; a non-null amount issues a partial refund for that amount only (Fase F,
        /// one seat of a multi-seat order cancelled).</summary>
        Task RefundAsync(string externalOrderId, Money? amount, CancellationToken cancellationToken);
    }
}
