namespace Checkbus.Web.Services;

/// <summary>
/// Closed outcome shared by the two "do the thing, no response body" driver-requirement actions —
/// <c>POST .../document</c> (upload) and <c>PUT .../status</c> (validate) — so callers can
/// pattern-match on the result without inspecting HTTP status codes directly. Both endpoints
/// return <c>204 No Content</c> on success, so a single hierarchy covers both; <see
/// cref="ValidationFailed"/> realistically only fires for the upload endpoint (FluentValidation on
/// content type/size/past date), but is plumbed through for the validate endpoint too in case a
/// future contract change introduces one there. The private constructor confines the hierarchy to
/// the nested cases declared here; a consumer can exhaustively switch over <see cref="Success"/>,
/// <see cref="ValidationFailed"/>, <see cref="Forbidden"/>, <see cref="NotFound"/>, and
/// <see cref="TransportError"/>.
/// </summary>
public abstract record DriverRequirementActionOutcome
{
    private DriverRequirementActionOutcome()
    {
    }

    /// <summary>204 No Content — the action completed successfully.</summary>
    public sealed record Success : DriverRequirementActionOutcome;

    /// <summary>400 Bad Request — FluentValidation failures keyed by property name.</summary>
    public sealed record ValidationFailed(IReadOnlyDictionary<string, string[]> Errors) : DriverRequirementActionOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : DriverRequirementActionOutcome;

    /// <summary>404 Not Found — the target user does not exist or is outside the caller's organization.</summary>
    public sealed record NotFound : DriverRequirementActionOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : DriverRequirementActionOutcome;
}
