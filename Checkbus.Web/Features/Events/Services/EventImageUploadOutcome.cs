namespace Checkbus.Web.Features.Events.Services;

/// <summary>
/// Closed outcome of a <c>POST /api/Events/{id}/image</c> call, covering every realistic server
/// response so callers can pattern-match on the result without inspecting HTTP status codes
/// directly. The endpoint returns <c>204 No Content</c> on success (no response body), mirroring
/// <see cref="Checkbus.Web.Features.VehicleDocuments.Services.VehicleDocumentActionOutcome"/>'s
/// no-body-success shape — kept as its own type rather than reusing <see cref="EventActionOutcome"/>
/// since that one's <c>Success</c> carries an <c>EventResponse</c> body a 204 response doesn't have.
/// The private constructor confines the hierarchy to the nested cases declared here.
/// </summary>
public abstract record EventImageUploadOutcome
{
    private EventImageUploadOutcome()
    {
    }

    /// <summary>204 No Content — the image was uploaded successfully.</summary>
    public sealed record Success : EventImageUploadOutcome;

    /// <summary>400 Bad Request — FluentValidation failures keyed by property name.</summary>
    public sealed record ValidationFailed(IReadOnlyDictionary<string, string[]> Errors) : EventImageUploadOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : EventImageUploadOutcome;

    /// <summary>404 Not Found — the target event does not exist.</summary>
    public sealed record NotFound : EventImageUploadOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : EventImageUploadOutcome;
}
