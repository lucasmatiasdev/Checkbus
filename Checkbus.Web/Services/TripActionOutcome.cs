using Checkbus.Web.Contracts;

namespace Checkbus.Web.Services;

/// <summary>
/// Closed outcome of a <c>POST /api/Trips</c> call, covering every realistic server response so
/// callers can pattern-match on the result without inspecting HTTP status codes directly. Mirrors
/// <see cref="EventActionOutcome"/>'s shape. <see cref="ValidationFailed"/> also covers an
/// eligibility rejection (server-side 400 built as a ValidationProblemDetails by
/// TripExceptionHandler) — its message is the useful "why" and must be surfaced directly, never
/// replaced with a generic string. The private constructor confines the hierarchy to the nested
/// cases declared here.
/// </summary>
public abstract record TripActionOutcome
{
    private TripActionOutcome()
    {
    }

    /// <summary>201 Created — the trip was created successfully.</summary>
    public sealed record Success(TripResponse Trip) : TripActionOutcome;

    /// <summary>
    /// 400 Bad Request — FluentValidation failures keyed by property name, OR an eligibility
    /// rejection keyed under "Habilitacion" (see <see cref="Checkbus.ApiService.ExceptionHandling.TripExceptionHandler"/>).
    /// </summary>
    public sealed record ValidationFailed(IReadOnlyDictionary<string, string[]> Errors) : TripActionOutcome;

    /// <summary>404 Not Found — the referenced vehicle, driver, or event does not exist.</summary>
    public sealed record NotFound : TripActionOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : TripActionOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : TripActionOutcome;
}
