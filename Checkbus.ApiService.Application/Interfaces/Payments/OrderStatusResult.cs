namespace Checkbus.ApiService.Application.Interfaces.Payments
{
    /// <summary>Authoritative order status as read straight from Mercado Pago (never from a
    /// webhook payload's own status field — see <see cref="IPaymentService.GetOrderAsync"/>'s
    /// doc comment).</summary>
    public sealed record OrderStatusResult(string OrderId, MercadoPagoOrderStatus Status);
}
