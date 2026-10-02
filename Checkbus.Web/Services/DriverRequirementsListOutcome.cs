using Checkbus.Web.Contracts;

namespace Checkbus.Web.Services;

/// <summary>
/// Closed outcome of a <c>GET /api/DriverRequirements/{targetUserId}</c> call, covering every
/// realistic server response so callers can pattern-match on the result without inspecting HTTP
/// status codes directly. Unlike <see cref="UsersListOutcome"/>, 404 is a real, distinct case
/// here — the backend uses it for admin cross-organization lookups (IDOR-safe: a non-existent or
/// out-of-org target returns the same NotFound as a real one, never leaking existence) — so it is
/// never collapsed into <see cref="Forbidden"/>. The private constructor confines the hierarchy to
/// the nested cases declared here; a consumer can exhaustively switch over <see cref="Success"/>,
/// <see cref="Forbidden"/>, <see cref="NotFound"/>, and <see cref="TransportError"/>.
/// </summary>
public abstract record DriverRequirementsListOutcome
{
    private DriverRequirementsListOutcome()
    {
    }

    /// <summary>200 OK — the target user's driver requirements were fetched successfully.</summary>
    public sealed record Success(IReadOnlyList<DriverRequirementResponse> Requirements) : DriverRequirementsListOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : DriverRequirementsListOutcome;

    /// <summary>404 Not Found — the target user does not exist or is outside the caller's organization.</summary>
    public sealed record NotFound : DriverRequirementsListOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : DriverRequirementsListOutcome;
}
