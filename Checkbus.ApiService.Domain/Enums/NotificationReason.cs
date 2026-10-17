namespace Checkbus.ApiService.Domain.Enums
{
    // CU-04: same enum-of-reasons-with-"Otro" shape as TripCancellationReason, but unlike that
    // one the free-text Message on CreateNotificationCommand/Notification is ALWAYS required
    // regardless of which reason is picked — see Notification's own doc comment for why.
    public enum NotificationReason
    {
        CambioHorario,
        CambioVehiculo,
        Otro
    }
}
