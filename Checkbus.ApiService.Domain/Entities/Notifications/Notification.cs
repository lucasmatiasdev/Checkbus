using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Domain.Entities.Notifications
{
    // CU-04 (Publicar notificación): a standalone, manually-triggered record of a notification a
    // Planificador/Administrador sent to a scoped audience of passengers — never an automatic
    // side effect of another command (e.g. UpdateStopTimesCommandHandler never creates one of
    // these itself; see odd/tasks/notificaciones-viaje.md "Decisiones del usuario"). FK-only, no
    // navigation properties, same posture as every other entity in this codebase.
    //
    // TargetType/TargetId together resolve the audience: Trip targets exactly that Trip's own
    // Confirmada passengers; Event targets every Trip tied to that Event (and therefore every one
    // of those Trips' Confirmada passengers) — see INotificationRepository's history-read method
    // for how a Trip's "relevant" notifications are resolved back from this pair.
    //
    // Message is `required` unconditionally — unlike TripCancellationReason.Description (only
    // required when Reason == Otro), a notification without a message is useless no matter which
    // Reason it carries, so there is no corresponding "only required when Otro" validator rule.
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
