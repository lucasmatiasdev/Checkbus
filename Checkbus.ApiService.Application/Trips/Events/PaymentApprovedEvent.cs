using MediatR;

namespace Checkbus.ApiService.Application.Trips.Events
{

    public class PaymentApprovedEvent : INotification
    {
        public required Guid PaymentId { get; set; }
        public required IReadOnlyList<Guid> ReservationIds { get; set; }
    }
}
