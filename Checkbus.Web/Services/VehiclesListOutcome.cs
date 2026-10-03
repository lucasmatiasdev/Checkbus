using Checkbus.Web.Contracts;

namespace Checkbus.Web.Services;

/// <summary>
/// Closed outcome of a <c>GET /api/Vehicles</c> call, covering every realistic server response so
/// callers (the vehicle list Razor page) can pattern-match on the result without inspecting HTTP
/// status codes directly. Mirrors <see cref="UsersListOutcome"/>'s shape — this is "list my org's
/// vehicles", not a per-target lookup, so there is no distinct 404 case. The private constructor
/// confines the hierarchy to the nested cases declared here; a consumer can exhaustively switch
/// over <see cref="Success"/>, <see cref="Forbidden"/>, and <see cref="TransportError"/>.
/// </summary>
public abstract record VehiclesListOutcome
{
    private VehiclesListOutcome()
    {
    }

    /// <summary>200 OK — the organization's vehicles were fetched successfully.</summary>
    public sealed record Success(IReadOnlyList<VehicleResponse> Vehicles) : VehiclesListOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : VehiclesListOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : VehiclesListOutcome;
}
