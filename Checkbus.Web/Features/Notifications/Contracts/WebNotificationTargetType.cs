namespace Checkbus.Web.Features.Notifications.Contracts;

/// <summary>
/// Web-side mirror of <c>Checkbus.ApiService.Domain.Enums.NotificationTargetType</c>. Checkbus.Web
/// does not reference the API projects (pure BFF), so this enum is duplicated here rather than
/// shared. Member names and underlying numeric values must stay identical to the real
/// <c>NotificationTargetType</c> enum — no <c>JsonStringEnumConverter</c> is registered anywhere,
/// so the wire value is a plain integer that depends on member order matching exactly (same
/// convention as <see cref="Checkbus.Web.Features.Trips.Contracts.WebTripCancellationReason"/>).
/// </summary>
public enum WebNotificationTargetType
{
    Trip,
    Event
}
