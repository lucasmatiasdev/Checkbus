namespace Checkbus.ApiService.Domain.Enums
{
    // CU-04: the scope a Notification is addressed to. Trip targets one specific Trip's
    // Confirmada passengers; Event targets every Trip tied to that Event (and therefore every
    // one of those Trips' own Confirmada passengers). A new scope later (e.g. a whole
    // Organization) is one more enum value plus one more resolver in
    // CreateNotificationCommandHandler, not a redesign.
    public enum NotificationTargetType
    {
        Trip,
        Event
    }
}
