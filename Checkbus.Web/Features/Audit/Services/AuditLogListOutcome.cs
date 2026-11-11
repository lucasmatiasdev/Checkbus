using Checkbus.Web.Features.Audit.Contracts;

namespace Checkbus.Web.Features.Audit.Services;

/// <summary>
/// Closed outcome of a <c>GET /api/audit-log</c> call, covering every realistic server response
/// so callers (the "Auditoría" Razor page, CU-16) can pattern-match on the result without
/// inspecting HTTP status codes directly. Mirrors <see cref="Checkbus.Web.Features.Users.Services.UsersListOutcome"/>'s
/// shape — a plain read with no validation/conflict case, so only success, an auth failure, and a
/// transport failure are realistic. The private constructor confines the hierarchy to the nested
/// cases declared here; a consumer can exhaustively switch over <see cref="Success"/>,
/// <see cref="Forbidden"/>, and <see cref="TransportError"/>.
/// </summary>
public abstract record AuditLogListOutcome
{
    private AuditLogListOutcome()
    {
    }

    /// <summary>200 OK — the audit log entries for the caller's scope were fetched successfully.</summary>
    public sealed record Success(IReadOnlyList<AuditLogResponse> Entries) : AuditLogListOutcome;

    /// <summary>
    /// 401 Unauthorized or 403 Forbidden — covers CU-16 FA1's "authenticated user has neither
    /// Administrador nor CheckbusSoporte role" case. No reliable body to parse for either status.
    /// </summary>
    public sealed record Forbidden : AuditLogListOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc) — covers CU-16 FA1's "network or server error" case. Message is short and
    /// user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : AuditLogListOutcome;
}
