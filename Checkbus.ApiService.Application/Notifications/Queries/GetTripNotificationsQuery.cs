using MediatR;

namespace Checkbus.ApiService.Application.Notifications.Queries
{
    // CU-04: history for a Trip's detail screen — direct Trip-scoped notifications PLUS
    // Event-scoped ones that also reached this Trip's passengers (INotificationRepository
    // .GetRelevantToTripAsync).
    public class GetTripNotificationsQuery : IRequest<IReadOnlyList<NotificationDto>>
    {
        public required Guid TripId { get; set; }
    }
}
