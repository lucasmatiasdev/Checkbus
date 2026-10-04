using Checkbus.Web.Contracts;

namespace Checkbus.Web.Services;

/// <summary>
/// Closed outcome of a <c>GET /api/Viajes/{id}</c> call, covering every realistic server response
/// so callers can pattern-match on the result without inspecting HTTP status codes directly.
/// Mirrors <see cref="Checkbus.Web.Services.MaintenanceRecordOutcome"/>'s shape (single-item
/// get, including a distinct <see cref="NotFound"/> case).
/// </summary>
public abstract record ViajeOutcome
{
    private ViajeOutcome()
    {
    }

    /// <summary>200 OK — the viaje was fetched successfully.</summary>
    public sealed record Success(ViajeDetailResponse Viaje) : ViajeOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : ViajeOutcome;

    /// <summary>404 Not Found — no viaje exists with the given id in the caller's organization.</summary>
    public sealed record NotFound : ViajeOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : ViajeOutcome;
}
