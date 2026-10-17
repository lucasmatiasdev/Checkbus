namespace Checkbus.Web.Features.Notifications.Contracts;

/// <summary>
/// Web-side mirror of the API's <c>Checkbus.ApiService.Application.Notifications.Commands
/// .CreateNotificationCommand</c>, the request body for <c>POST api/Notifications</c> (CU-04).
/// </summary>
public sealed record CreateNotificationRequest
{
    public required WebNotificationTargetType TargetType { get; init; }
    public required Guid TargetId { get; init; }
    public required WebNotificationReason Reason { get; init; }
    public required string Message { get; init; }
}
