using Checkbus.ApiService.Domain.Entities.Passengers;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    // Added for R5 (odd/tasks/reservas-publicas.md). Passenger is written as part of
    // CreatePublicReservationCommandHandler's batch (via IReservationRepository.CreateBatchAsync)
    // and was never read back by id until now — the public confirmation page needs the
    // Passenger's own display fields (Name/Surname/DocumentType/DocumentNumber) for each
    // resolved Reservation.
    public interface IPassengerRepository
    {
        Task<Passenger?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    }
}
