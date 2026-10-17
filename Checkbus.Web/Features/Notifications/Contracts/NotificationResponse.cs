namespace Checkbus.Web.Features.Notifications.Contracts;

/// <summary>
/// Web-side mirror of the API's <c>Checkbus.ApiService.Application.Notifications.Queries
/// .NotificationDto</c>, one entry returned by <c>GET api/Notifications?tripId={id}</c> (CU-04).
/// </summary>
public sealed record NotificationResponse
{
    public Guid Id { get; init; }
    public WebNotificationTargetType TargetType { get; init; }
    public Guid TargetId { get; init; }
    public WebNotificationReason Reason { get; init; }
    public required string Message { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CreatedByName { get; init; }
}
