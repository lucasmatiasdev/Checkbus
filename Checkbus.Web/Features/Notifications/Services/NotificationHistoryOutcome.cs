using Checkbus.Web.Features.Notifications.Contracts;

namespace Checkbus.Web.Features.Notifications.Services;

/// <summary>
/// Closed outcome of a <c>GET api/Notifications?tripId={id}</c> call (CU-04). Mirrors
/// <see cref="Checkbus.Web.Features.Trips.Services.TripPendingRefundsOutcome"/>'s shape — a Trip
/// with zero notifications sent resolves as <see cref="Success"/> with an empty list, never
/// <see cref="NotFound"/>; <see cref="NotFound"/> is reserved for when the Trip itself doesn't
/// resolve (doesn't exist, or belongs to another organization).
/// </summary>
public abstract record NotificationHistoryOutcome
{
    private NotificationHistoryOutcome()
    {
    }

    /// <summary>200 OK — the Trip's relevant notification history (possibly empty).</summary>
    public sealed record Success(IReadOnlyList<NotificationResponse> Notifications) : NotificationHistoryOutcome;

    /// <summary>404 Not Found — the trip doesn't exist or is outside the caller's organization.</summary>
    public sealed record NotFound : NotificationHistoryOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : NotificationHistoryOutcome;

    /// <summary>Any transport-level failure, short and user-presentable.</summary>
    public sealed record TransportError(string Message) : NotificationHistoryOutcome;
}
