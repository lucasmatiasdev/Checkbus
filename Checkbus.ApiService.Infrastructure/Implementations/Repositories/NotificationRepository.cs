using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Notifications;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.ApiService.Infrastructure.Implementations.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly CheckbusDbContext _context;

        public NotificationRepository(CheckbusDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Notification notification, CancellationToken cancellationToken)
        {
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Union of two independent conditions — see INotificationRepository's own doc comment:
        // rows that target this Trip directly, OR rows that target its Event.
        public async Task<IReadOnlyList<Notification>> GetRelevantToTripAsync(
            Guid tripId, Guid eventId, CancellationToken cancellationToken)
        {
            return await _context.Notifications
                .Where(n =>
                    (n.TargetType == NotificationTargetType.Trip && n.TargetId == tripId) ||
                    (n.TargetType == NotificationTargetType.Event && n.TargetId == eventId))
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync(cancellationToken);
        }
    }
}
