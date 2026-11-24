using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Domain.Entities.Notifications
{

    public class Notification
    {
        public Guid Id { get; set; }
        public required Guid OrganizationId { get; set; }
        public NotificationTargetType TargetType { get; set; }
        public required Guid TargetId { get; set; }
        public NotificationReason Reason { get; set; }
        public required string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public required Guid CreatedByUserId { get; set; }
    }
}
