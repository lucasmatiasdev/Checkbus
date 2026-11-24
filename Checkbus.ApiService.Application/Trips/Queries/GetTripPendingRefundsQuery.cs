using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{

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
