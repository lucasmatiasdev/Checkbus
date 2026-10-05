using Checkbus.Web.Contracts;

namespace Checkbus.Web.Services;

/// <summary>
/// Closed outcome of a <c>GET /api/Trips/{id}</c> call, covering every realistic server response
/// so callers can pattern-match on the result without inspecting HTTP status codes directly.
/// Mirrors <see cref="Checkbus.Web.Services.MaintenanceRecordOutcome"/>'s shape (single-item
/// get, including a distinct <see cref="NotFound"/> case).
/// </summary>
public abstract record TripOutcome
{
    private TripOutcome()
    {
    }

    /// <summary>200 OK — the trip was fetched successfully.</summary>
    public sealed record Success(TripDetailResponse Trip) : TripOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : TripOutcome;

    /// <summary>404 Not Found — no trip exists with the given id in the caller's organization.</summary>
    public sealed record NotFound : TripOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : TripOutcome;
}
