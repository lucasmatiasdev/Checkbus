using Checkbus.Web.Features.Trips.Contracts;

namespace Checkbus.Web.Features.Boarding.Services;

/// <summary>
/// Closed outcome of a <c>GET /api/boarding/my-trips</c> call. Mirrors <see
/// cref="Checkbus.Web.Features.Trips.Services.TripsListOutcome"/>'s shape — reuses
/// <see cref="TripResponse"/> since the response body is the same TripDto shape.
/// </summary>
public abstract record DriverTripsOutcome
{
    private DriverTripsOutcome()
    {
    }

    /// <summary>200 OK — the caller's own published trips were fetched successfully.</summary>
    public sealed record Success(IReadOnlyList<TripResponse> Trips) : DriverTripsOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : DriverTripsOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : DriverTripsOutcome;
}
