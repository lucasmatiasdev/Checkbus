namespace Checkbus.Web.Services;

/// <summary>
/// Closed outcome of a <c>GET /api/DriverRequirements/expiring-count</c> call, covering every
/// realistic server response so callers (the dashboard card) can pattern-match on the result
/// without inspecting HTTP status codes directly. This endpoint is Administrador-only at the
/// controller's <c>[Authorize(Roles = ...)]</c> attribute, so there is no distinct 404 case. The
/// private constructor confines the hierarchy to the nested cases declared here; a consumer can
/// exhaustively switch over <see cref="Success"/>, <see cref="Forbidden"/>, and
/// <see cref="TransportError"/>.
/// </summary>
public abstract record DriverRequirementCountOutcome
{
    private DriverRequirementCountOutcome()
    {
    }

    /// <summary>200 OK — the combined "por vencer" + "vencido" count was fetched successfully.</summary>
    public sealed record Success(int Count) : DriverRequirementCountOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : DriverRequirementCountOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : DriverRequirementCountOutcome;
}
