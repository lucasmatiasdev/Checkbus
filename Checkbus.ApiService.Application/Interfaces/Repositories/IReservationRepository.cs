using Checkbus.ApiService.Domain.Entities.Reservations;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IReservationRepository
    {
        // Any Status (right now only Confirmada exists) — used by
        // GetPublicTripSeatMapQueryHandler to compute which Seat.Numbers are occupied for a
        // published Trip. R4 (reservation create) will extend this interface with write
        // operations when it lands.
        Task<IReadOnlyList<Reservation>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken);
    }
}
