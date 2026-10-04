using Checkbus.Web.Contracts;

namespace Checkbus.Web.Services;

/// <summary>
/// Closed outcome of a <c>GET /api/Viajes</c> call, covering every realistic server response so
/// callers can pattern-match on the result without inspecting HTTP status codes directly. Mirrors
/// <see cref="EventosListOutcome"/>'s shape.
/// </summary>
public abstract record ViajesListOutcome
{
    private ViajesListOutcome()
    {
    }

    /// <summary>200 OK — the viajes were fetched successfully.</summary>
    public sealed record Success(IReadOnlyList<ViajeResponse> Viajes) : ViajesListOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : ViajesListOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : ViajesListOutcome;
}
