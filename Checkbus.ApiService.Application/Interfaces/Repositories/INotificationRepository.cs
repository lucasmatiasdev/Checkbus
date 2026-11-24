using Checkbus.ApiService.Domain.Entities.Notifications;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface INotificationRepository
    {
        Task AddAsync(Notification notification, CancellationToken cancellationToken);

        Task<IReadOnlyList<Notification>> GetRelevantToTripAsync(
            Guid tripId, Guid eventId, CancellationToken cancellationToken);
    }
}
