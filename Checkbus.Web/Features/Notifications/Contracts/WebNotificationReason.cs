namespace Checkbus.Web.Features.Notifications.Contracts;

/// <summary>
/// Web-side mirror of <c>Checkbus.ApiService.Domain.Enums.NotificationReason</c>. Member names
/// and underlying numeric values must stay identical to the real <c>NotificationReason</c> enum —
/// no <c>JsonStringEnumConverter</c> is registered anywhere, so the wire value is a plain integer
/// that depends on member order matching exactly (same convention as
/// <see cref="Checkbus.Web.Features.Trips.Contracts.WebTripCancellationReason"/>).
/// </summary>
public enum WebNotificationReason
{
    CambioHorario,
    CambioVehiculo,
    Otro
}
