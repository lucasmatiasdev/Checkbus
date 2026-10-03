using Checkbus.Web.Contracts;

namespace Checkbus.Web.Services;

/// <summary>
/// Closed outcome of a <c>GET /api/MaintenanceRecords</c> call, covering every realistic server
/// response so callers can pattern-match on the result without inspecting HTTP status codes
/// directly. Mirrors <see cref="VehicleDocumentsListOutcome"/>'s shape, except there is no distinct
/// <c>NotFound</c> case here — the handler's optional <c>VehicleId</c> filter is always org-scoped
/// and simply yields an empty list rather than a 404 for an out-of-org vehicle. The private
/// constructor confines the hierarchy to the nested cases declared here; a consumer can
/// exhaustively switch over <see cref="Success"/>, <see cref="Forbidden"/>, and
/// <see cref="TransportError"/>.
/// </summary>
public abstract record MaintenanceRecordsListOutcome
{
    private MaintenanceRecordsListOutcome()
    {
    }

    /// <summary>200 OK — the matching maintenance records were fetched successfully.</summary>
    public sealed record Success(IReadOnlyList<MaintenanceRecordResponse> Records) : MaintenanceRecordsListOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : MaintenanceRecordsListOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : MaintenanceRecordsListOutcome;
}
