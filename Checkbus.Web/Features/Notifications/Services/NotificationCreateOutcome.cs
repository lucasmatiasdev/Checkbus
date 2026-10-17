namespace Checkbus.Web.Features.Notifications.Services;

/// <summary>
/// Closed outcome of a <c>POST api/Notifications</c> call (CU-04), covering every realistic
/// server response so callers can pattern-match on the result without inspecting HTTP status
/// codes directly. Mirrors <see cref="Checkbus.Web.Features.Trips.Services.TripCancelOutcome"/>'s
/// no-body-success shape, except the success status is 201 Created (the controller's
/// <c>NotificationsController.Create</c> returns <c>StatusCode(StatusCodes.Status201Created)</c>
/// with no body — <c>CreateNotificationCommand</c> is an <c>IRequest</c> with no response).
/// <see cref="ValidationFailed"/> covers FluentValidation failures from
/// <c>CreateNotificationCommandValidator</c>. The private constructor confines the hierarchy to
/// the nested cases declared here.
/// </summary>
public abstract record NotificationCreateOutcome
{
    private NotificationCreateOutcome()
    {
    }

    /// <summary>201 Created — the notification was persisted and emails were dispatched.</summary>
    public sealed record Success : NotificationCreateOutcome;

    /// <summary>400 Bad Request — FluentValidation failures keyed by property name.</summary>
    public sealed record ValidationFailed(IReadOnlyDictionary<string, string[]> Errors) : NotificationCreateOutcome;

    /// <summary>
    /// 404 Not Found — the referenced Trip or Event doesn't exist or is outside the caller's
    /// organization (IDOR-safe: cross-tenant existence is never leaked to the caller).
    /// </summary>
    public sealed record NotFound : NotificationCreateOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : NotificationCreateOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : NotificationCreateOutcome;
}
