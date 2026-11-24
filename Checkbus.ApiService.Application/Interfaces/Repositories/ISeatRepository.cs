using Checkbus.ApiService.Domain.Entities.Seats;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface ISeatRepository
    {

        Task EnsureSeatsForVehicleAsync(Guid vehicleId, int capacity, CancellationToken cancellationToken);

        Task<IReadOnlyList<Seat>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken);
    }
}
