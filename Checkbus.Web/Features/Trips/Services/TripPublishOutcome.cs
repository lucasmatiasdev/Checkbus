namespace Checkbus.Web.Features.Trips.Services;

/// <summary>
/// Closed outcome of a <c>POST /api/Trips/{id}/publish</c> call, covering every realistic server
/// response so callers can pattern-match on the result without inspecting HTTP status codes
/// directly. The endpoint returns <c>204 No Content</c> on success (no response body), mirroring
/// <see cref="Checkbus.Web.Features.MaintenanceRecords.Services.MaintenanceRecordUpdateOutcome"/>'s
/// no-body-success shape. <see cref="ValidationFailed"/> also covers the invalid-state-transition
/// rejection (server-side 400 built as a ValidationProblemDetails by
/// <see cref="Checkbus.ApiService.ExceptionHandling.TripExceptionHandler"/>) — its message is the
/// useful "why" and must be surfaced directly, never replaced with a generic string. The private
/// constructor confines the hierarchy to the nested cases declared here.
/// </summary>
public abstract record TripPublishOutcome
{
    private TripPublishOutcome()
    {
    }

    /// <summary>204 No Content — the trip was published successfully.</summary>
    public sealed record Success : TripPublishOutcome;

    /// <summary>
    /// 400 Bad Request — the trip is not currently Programado, keyed under "Habilitacion" (see
    /// <see cref="Checkbus.ApiService.ExceptionHandling.TripExceptionHandler"/>).
    /// </summary>
    public sealed record ValidationFailed(IReadOnlyDictionary<string, string[]> Errors) : TripPublishOutcome;

    /// <summary>404 Not Found — the trip does not exist or is outside the caller's organization.</summary>
    public sealed record NotFound : TripPublishOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : TripPublishOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : TripPublishOutcome;
}
