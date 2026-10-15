namespace Checkbus.ApiService.Application.Interfaces.Payments
{
    /// <summary><see cref="OrderId"/> is Mercado Pago's own order id — persisted as
    /// <c>Payment.ExternalOrderId</c>. <see cref="CheckoutUrl"/> is nullable: the Orders API's
    /// <c>type: "online"</c> resource does not always carry a dedicated hosted-checkout redirect
    /// URL in the strongly-typed SDK response at creation time (see
    /// <c>MercadoPagoPaymentService</c>'s doc comment for how the adapter tries to recover one
    /// anyway) — Fase C must handle a null <see cref="CheckoutUrl"/>.</summary>
    public sealed record CreateOrderResult(string OrderId, string? CheckoutUrl);
}
