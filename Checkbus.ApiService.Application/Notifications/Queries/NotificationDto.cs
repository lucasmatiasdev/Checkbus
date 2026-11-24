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

        public string? CreatedByName { get; set; }
    }
}
