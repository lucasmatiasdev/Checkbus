namespace Checkbus.Web.Services;

/// <summary>
/// Closed outcome shared by the two "do the thing, no response body" vehicle-document actions —
/// <c>POST .../document</c> (upload) and <c>PUT .../status</c> (validate) — so callers can
/// pattern-match on the result without inspecting HTTP status codes directly. Both endpoints return
/// <c>204 No Content</c> on success, so a single hierarchy covers both. Mirrors
/// <see cref="DriverRequirementActionOutcome"/>'s shape exactly. <see cref="NotFound"/> covers two
/// distinct server-side causes that collapse to the same plain 404 with no body distinction — the
/// vehicle doesn't exist/is out-of-org (<c>VehicleNotFoundException</c>), or (for validate calls
/// only) this document type was never uploaded (<c>VehicleDocumentNotFoundException</c>) — there is
/// nothing in the response to distinguish them by, so this client does not attempt to. The private
/// constructor confines the hierarchy to the
/// nested cases declared here; a consumer can exhaustively switch over <see cref="Success"/>,
/// <see cref="ValidationFailed"/>, <see cref="Forbidden"/>, <see cref="NotFound"/>, and
/// <see cref="TransportError"/>.
/// </summary>
public abstract record VehicleDocumentActionOutcome
{
    private VehicleDocumentActionOutcome()
    {
    }

    /// <summary>204 No Content — the action completed successfully.</summary>
    public sealed record Success : VehicleDocumentActionOutcome;

    /// <summary>400 Bad Request — FluentValidation failures keyed by property name.</summary>
    public sealed record ValidationFailed(IReadOnlyDictionary<string, string[]> Errors) : VehicleDocumentActionOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : VehicleDocumentActionOutcome;

    /// <summary>
    /// 404 Not Found — the vehicle does not exist/is out-of-org, or (validate only) the document
    /// type was never uploaded. The two causes are indistinguishable from the response alone.
    /// </summary>
    public sealed record NotFound : VehicleDocumentActionOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : VehicleDocumentActionOutcome;
}
