namespace Checkbus.ApiService.Domain.Enums
{
    // Pendiente is the initial state right after IPaymentService.CreateOrderAsync succeeds and
    // before Mercado Pago's webhook confirms anything (odd/tasks/pagos-mercadopago.md Fase A/B).
    // Aprobado/Rechazado are the two terminal outcomes the webhook reports (Fase D).
    // Reembolsado/ParcialmenteReembolsado arrive later via cancellation (Fase F) —
    // ParcialmenteReembolsado covers a Payment that groups several seats where only some of the
    // underlying Reservations have been cancelled/refunded so far; Reembolsado is every seat on
    // that Payment refunded (or the whole order refunded at once, e.g. the orphaned-approval
    // path in Fase D).
    public enum PaymentStatus
    {
        Pendiente,
        Aprobado,
        Rechazado,
        Reembolsado,
        ParcialmenteReembolsado
    }
}
