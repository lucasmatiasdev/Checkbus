using Checkbus.ApiService.Domain.Entities.BoardingRecords;
using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IBoardingRecordRepository
    {

        Task EnsureForTripAsync(Guid tripId, CancellationToken cancellationToken);

        Task<IReadOnlyList<BoardingRecord>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken);

        Task<BoardingRecord?> GetByReservationIdAsync(Guid reservationId, CancellationToken cancellationToken);

        Task ValidateAsync(Guid reservationId, ValidationMethod method, CancellationToken cancellationToken);

        Task CloseBoardingAsync(Guid tripId, CancellationToken cancellationToken);
    }
}
