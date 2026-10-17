using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.Notifications.Commands
{
    // CU-04 (Publicar notificación): a Planificador/Administrador explicitly picks an audience
    // (TargetType+TargetId), a Reason and a free-text Message, and the system persists the
    // record and emails every affected Confirmada passenger. Deliberately NOT a side effect of
    // any other command — see odd/tasks/notificaciones-viaje.md "Decisiones del usuario".
    // No meaningful response body, same shape as CancelTripCommand/PublishTripCommand.
    public class CreateNotificationCommand : IRequest
    {
        public required NotificationTargetType TargetType { get; set; }
        public required Guid TargetId { get; set; }
        public required NotificationReason Reason { get; set; }

        // Always required regardless of Reason — see CreateNotificationCommandValidator and
        // Notification's own doc comment for why this differs from
        // CancelTripCommand.Description's "only required when Otro" rule.
        public required string Message { get; set; }
    }
}
