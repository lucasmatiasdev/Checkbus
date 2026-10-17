using Checkbus.ApiService.Domain.Entities.Notifications;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface INotificationRepository
    {
        Task AddAsync(Notification notification, CancellationToken cancellationToken);

        // Resolves every Notification "relevant" to one Trip: rows that target that Trip
        // DIRECTLY (TargetType=Trip, TargetId=tripId) OR that target its Event
        // (TargetType=Event, TargetId=eventId) — an Event-scoped notification also reached that
        // Trip's own Confirmada passengers, so it belongs in that Trip's history just as much as
        // a Trip-scoped one does. This is a union of two independent conditions, not a single FK
        // lookup — the caller passes the Trip's own EventId alongside its Id precisely because
        // Notification has no navigation back to Trip to derive it from.
        Task<IReadOnlyList<Notification>> GetRelevantToTripAsync(
            Guid tripId, Guid eventId, CancellationToken cancellationToken);
    }
}
