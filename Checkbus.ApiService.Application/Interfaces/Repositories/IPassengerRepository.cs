using Checkbus.ApiService.Domain.Entities.Passengers;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{

    public interface IPassengerRepository
    {
        Task<Passenger?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    }
}
