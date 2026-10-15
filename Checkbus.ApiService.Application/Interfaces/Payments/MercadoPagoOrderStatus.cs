namespace Checkbus.ApiService.Application.Interfaces.Payments
{
    /// <summary>Normalized Mercado Pago order/payment status, already collapsed from whatever
    /// raw status string the Orders API returns (e.g. "accredited" vs "approved", "canceled" vs
    /// "cancelled") — Fase D's webhook handler maps this to our own <c>PaymentStatus</c>
    /// (Aprobado/Rechazado/Reembolsado), it never sees Mercado Pago's raw vocabulary.</summary>
    public enum MercadoPagoOrderStatus
    {
        Pending,
        Approved,
        Rejected,
        Refunded,
        Unknown
    }
}
