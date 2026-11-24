using MediatR;

namespace Checkbus.ApiService.Application.Notifications.Queries
{

    public class GetTripNotificationsQuery : IRequest<IReadOnlyList<NotificationDto>>
    {
        public required Guid TripId { get; set; }
    }
}
