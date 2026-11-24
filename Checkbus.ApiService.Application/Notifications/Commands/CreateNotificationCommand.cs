using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.Notifications.Commands
{

    public class CreateNotificationCommand : IRequest
    {
        public required NotificationTargetType TargetType { get; set; }
        public required Guid TargetId { get; set; }
        public required NotificationReason Reason { get; set; }

        public required string Message { get; set; }
    }
}
