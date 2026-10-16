using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    public class GetTripPendingRefundsQueryHandler : IRequestHandler<GetTripPendingRefundsQuery, IReadOnlyList<PendingRefundDto>>
    {
        private readonly ITripRepository _tripRepository;
        private readonly IReservationRepository _reservationRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly ICurrentUserService _currentUser;

        public GetTripPendingRefundsQueryHandler(
            ITripRepository tripRepository,
            IReservationRepository reservationRepository,
            IPaymentRepository paymentRepository,
            ICurrentUserService currentUser)
        {
            _tripRepository = tripRepository;
            _reservationRepository = reservationRepository;
            _paymentRepository = paymentRepository;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<PendingRefundDto>> Handle(
            GetTripPendingRefundsQuery request, CancellationToken cancellationToken)
        {
            var callerOrganizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var trip = await _tripRepository.GetByIdAsync(request.TripId, cancellationToken);
            if (trip is null || trip.OrganizationId != callerOrganizationId)
            {
                // IDOR-safe: cross-tenant existence is never leaked to the caller.
                throw new TripNotFoundException();
            }

            // Unlike GetByTripIdAsync, includes Cancelada rows — after a trip cancellation every
            // Reservation on it IS Cancelada, and that is exactly what this report looks at.
            var reservations = await _reservationRepository.GetAllByTripIdAsync(trip.Id, cancellationToken);

            var cancelledPaymentIds = reservations
                .Where(r => r.Status == ReservationStatus.Cancelada && r.PaymentId is not null)
                .GroupBy(r => r.PaymentId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(r => r.Id).ToList());

            var pendingRefunds = new List<PendingRefundDto>();

            foreach (var (paymentId, reservationIds) in cancelledPaymentIds)
            {
                var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);

                // Aprobado/ParcialmenteReembolsado means the gateway actually charged this
                // Payment at some point and the refund never reached Reembolsado — exactly the
                // "needs manual follow-up at the gateway" case this report exists for. Pendiente
                // (never charged) and Reembolsado (already fully refunded) are both non-issues.
                if (payment is not null && payment.Status is PaymentStatus.Aprobado or PaymentStatus.ParcialmenteReembolsado)
                {
                    pendingRefunds.Add(new PendingRefundDto
                    {
                        PaymentId = payment.Id,
                        ExternalOrderId = payment.ExternalOrderId,
                        Amount = payment.TotalAmount.Amount,
                        ReservationIds = reservationIds
                    });
                }
            }

            return pendingRefunds;
        }
    }
}
