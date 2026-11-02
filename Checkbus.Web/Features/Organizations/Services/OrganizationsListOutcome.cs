using Checkbus.Web.Features.Organizations.Contracts;

namespace Checkbus.Web.Features.Organizations.Services;

/// <summary>
/// Closed outcome of a <c>GET /api/organizations</c> call, covering every realistic server
/// response so callers (CU-16's "Auditoría" page [Organización] dropdown,
/// <c>odd/tasks/auditoria-sistema.md</c> T4) can pattern-match on the result without inspecting
/// HTTP status codes directly. A plain read with no validation/conflict case, so only success, an
/// auth failure, and a transport failure are realistic. The private constructor confines the
/// hierarchy to the nested cases declared here; a consumer can exhaustively switch over
/// <see cref="Success"/>, <see cref="Forbidden"/>, and <see cref="TransportError"/>.
/// </summary>
public abstract record OrganizationsListOutcome
{
    private OrganizationsListOutcome()
    {
    }

    /// <summary>200 OK — every organization was fetched successfully.</summary>
    public sealed record Success(IReadOnlyList<OrganizationListItemResponse> Organizations) : OrganizationsListOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : OrganizationsListOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : OrganizationsListOutcome;
}
