using Checkbus.ApiService.Domain.Entities.Passengers;
using Checkbus.ApiService.Domain.Entities.Payments;
using Checkbus.ApiService.Domain.Entities.Reservations;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IReservationRepository
    {

        Task<IReadOnlyList<Reservation>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken);

        Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

        Task CreateBatchAsync(
            Payment payment,
            IReadOnlyCollection<Passenger> passengers,
            IReadOnlyCollection<Reservation> reservations,
            CancellationToken cancellationToken);

        Task CancelAsync(Guid reservationId, CancellationToken cancellationToken);

        Task<IReadOnlyList<Reservation>> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken)
            => throw new NotSupportedException("GetByPaymentIdAsync is not implemented by this IReservationRepository.");

        Task ConfirmAsync(Guid reservationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("ConfirmAsync is not implemented by this IReservationRepository.");

        Task ReleaseExpiredHoldsAsync(Guid tripId, int holdExpirationMinutes, CancellationToken cancellationToken)
            => throw new NotSupportedException("ReleaseExpiredHoldsAsync is not implemented by this IReservationRepository.");

        Task<IReadOnlyList<Reservation>> GetAllByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("GetAllByTripIdAsync is not implemented by this IReservationRepository.");
    }
}
