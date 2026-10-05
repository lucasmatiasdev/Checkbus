using Checkbus.Web.Contracts;

namespace Checkbus.Web.Services;

/// <summary>
/// Closed outcome of a <c>GET /api/Trips</c> call, covering every realistic server response so
/// callers can pattern-match on the result without inspecting HTTP status codes directly. Mirrors
/// <see cref="EventsListOutcome"/>'s shape.
/// </summary>
public abstract record TripsListOutcome
{
    private TripsListOutcome()
    {
    }

    /// <summary>200 OK — the trips were fetched successfully.</summary>
    public sealed record Success(IReadOnlyList<TripResponse> Trips) : TripsListOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : TripsListOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : TripsListOutcome;
}
