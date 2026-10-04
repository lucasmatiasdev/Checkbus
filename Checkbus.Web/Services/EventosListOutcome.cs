using Checkbus.Web.Contracts;

namespace Checkbus.Web.Services;

/// <summary>
/// Closed outcome of a <c>GET /api/Eventos</c> call, covering every realistic server response so
/// callers can pattern-match on the result without inspecting HTTP status codes directly. Mirrors
/// <see cref="MaintenanceRecordsListOutcome"/>'s shape. The private constructor confines the
/// hierarchy to the nested cases declared here; a consumer can exhaustively switch over
/// <see cref="Success"/>, <see cref="Forbidden"/>, and <see cref="TransportError"/>.
/// </summary>
public abstract record EventosListOutcome
{
    private EventosListOutcome()
    {
    }

    /// <summary>200 OK — the eventos were fetched successfully.</summary>
    public sealed record Success(IReadOnlyList<EventoResponse> Eventos) : EventosListOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : EventosListOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : EventosListOutcome;
}
