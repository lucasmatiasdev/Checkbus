using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    // CU-05: lists, for a (typically cancelled) Trip, the Payments whose refund never actually
    // completed via the gateway — so Checkbus staff can follow up manually. No request fields
    // beyond TripId; tenant ownership is checked against the authenticated caller in the handler.
    public class GetTripPendingRefundsQuery : IRequest<IReadOnlyList<PendingRefundDto>>
    {
        public required Guid TripId { get; set; }
    }

    public class PendingRefundDto
    {
        public required Guid PaymentId { get; set; }
        public string? ExternalOrderId { get; set; }
        public required decimal Amount { get; set; }
        public required IReadOnlyList<Guid> ReservationIds { get; set; }
    }
}
