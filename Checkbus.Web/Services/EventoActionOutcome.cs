using Checkbus.Web.Contracts;

namespace Checkbus.Web.Services;

/// <summary>
/// Closed outcome of a <c>POST /api/Eventos</c> call, covering every realistic server response so
/// callers can pattern-match on the result without inspecting HTTP status codes directly. Mirrors
/// <see cref="MaintenanceRecordActionOutcome"/>'s shape, minus a distinct <c>NotFound</c> case —
/// Evento creation has no parent-resource lookup that could 404. The private constructor confines
/// the hierarchy to the nested cases declared here; a consumer can exhaustively switch over
/// <see cref="Success"/>, <see cref="ValidationFailed"/>, <see cref="Forbidden"/>, and
/// <see cref="TransportError"/>.
/// </summary>
public abstract record EventoActionOutcome
{
    private EventoActionOutcome()
    {
    }

    /// <summary>201 Created — the evento was created successfully.</summary>
    public sealed record Success(EventoResponse Evento) : EventoActionOutcome;

    /// <summary>400 Bad Request — FluentValidation failures keyed by property name.</summary>
    public sealed record ValidationFailed(IReadOnlyDictionary<string, string[]> Errors) : EventoActionOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : EventoActionOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : EventoActionOutcome;
}
