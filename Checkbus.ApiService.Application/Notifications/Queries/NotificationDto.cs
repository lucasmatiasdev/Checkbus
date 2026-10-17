using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Application.Notifications.Queries
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public NotificationTargetType TargetType { get; set; }
        public Guid TargetId { get; set; }
        public NotificationReason Reason { get; set; }
        public required string Message { get; set; }
        public DateTime CreatedAt { get; set; }

        // Resolved display name for CreatedByUserId, same "resolve a Guid FK to a display name
        // for the UI" pattern GetPublicReservationsConfirmationQueryHandler uses for a Trip's
        // driver. Null when the creating User can no longer be resolved.
        public string? CreatedByName { get; set; }
    }
}
