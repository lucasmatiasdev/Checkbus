using MediatR;

namespace Checkbus.ApiService.Application.Trips.Events
{
    // Fase D (odd/tasks/pagos-mercadopago.md) — raised by HandlePaymentWebhookCommandHandler
    // once a Payment's whole batch of Reservations has been confirmed and invoiced. Carries only
    // ids, not the resolved entities: PaymentApprovedEventHandler re-resolves Trip/Passenger/Seat
    // details itself, keeping this event a thin, decoupled signal rather than a payload the
    // publisher has to keep in sync with what every future subscriber might need.
    public class PaymentApprovedEvent : INotification
    {
        public required Guid PaymentId { get; set; }
        public required IReadOnlyList<Guid> ReservationIds { get; set; }
    }
}
