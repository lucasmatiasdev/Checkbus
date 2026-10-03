using Checkbus.Web.Contracts;

namespace Checkbus.Web.Services;

/// <summary>
/// Closed outcome of a <c>GET /api/MaintenanceRecords/{id}</c> call, covering every realistic
/// server response so callers can pattern-match on the result without inspecting HTTP status codes
/// directly. Mirrors <see cref="VehicleDocumentsListOutcome"/>'s shape — 404 is a real, distinct
/// case here (the record doesn't exist or is out-of-org), never collapsed into
/// <see cref="Forbidden"/>. The private constructor confines the hierarchy to the nested cases
/// declared here; a consumer can exhaustively switch over <see cref="Success"/>,
/// <see cref="Forbidden"/>, <see cref="NotFound"/>, and <see cref="TransportError"/>.
/// </summary>
public abstract record MaintenanceRecordOutcome
{
    private MaintenanceRecordOutcome()
    {
    }

    /// <summary>200 OK — the maintenance record was fetched successfully.</summary>
    public sealed record Success(MaintenanceRecordResponse Record) : MaintenanceRecordOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : MaintenanceRecordOutcome;

    /// <summary>404 Not Found — the record does not exist or is outside the caller's organization.</summary>
    public sealed record NotFound : MaintenanceRecordOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : MaintenanceRecordOutcome;
}
