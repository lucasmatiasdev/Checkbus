using Checkbus.Web.Contracts;

namespace Checkbus.Web.Services;

/// <summary>
/// Closed outcome of a <c>POST /api/Viajes</c> call, covering every realistic server response so
/// callers can pattern-match on the result without inspecting HTTP status codes directly. Mirrors
/// <see cref="EventoActionOutcome"/>'s shape. <see cref="ValidationFailed"/> also covers a
/// habilitación rejection (server-side 400 built as a ValidationProblemDetails by
/// ViajeExceptionHandler) — its message is the useful "why" and must be surfaced directly, never
/// replaced with a generic string. The private constructor confines the hierarchy to the nested
/// cases declared here.
/// </summary>
public abstract record ViajeActionOutcome
{
    private ViajeActionOutcome()
    {
    }

    /// <summary>201 Created — the viaje was created successfully.</summary>
    public sealed record Success(ViajeResponse Viaje) : ViajeActionOutcome;

    /// <summary>
    /// 400 Bad Request — FluentValidation failures keyed by property name, OR a habilitación
    /// rejection keyed under "Habilitacion" (see <see cref="Checkbus.ApiService.ExceptionHandling.ViajeExceptionHandler"/>).
    /// </summary>
    public sealed record ValidationFailed(IReadOnlyDictionary<string, string[]> Errors) : ViajeActionOutcome;

    /// <summary>404 Not Found — the referenced vehicle, chofer, or evento does not exist.</summary>
    public sealed record NotFound : ViajeActionOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : ViajeActionOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : ViajeActionOutcome;
}
