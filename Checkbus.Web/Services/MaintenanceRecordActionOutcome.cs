using Checkbus.Web.Contracts;

namespace Checkbus.Web.Services;

/// <summary>
/// Closed outcome of a <c>POST /api/MaintenanceRecords</c> call, covering every realistic server
/// response so callers can pattern-match on the result without inspecting HTTP status codes
/// directly. Mirrors <see cref="VehicleRegistrationOutcome"/>'s shape — a create action whose
/// success body carries the created resource. The private constructor confines the hierarchy to
/// the nested cases declared here; a consumer can exhaustively switch over <see cref="Success"/>,
/// <see cref="ValidationFailed"/>, <see cref="Forbidden"/>, <see cref="NotFound"/>, and
/// <see cref="TransportError"/>.
/// </summary>
public abstract record MaintenanceRecordActionOutcome
{
    private MaintenanceRecordActionOutcome()
    {
    }

    /// <summary>201 Created — the maintenance record was created successfully.</summary>
    public sealed record Success(MaintenanceRecordResponse Record) : MaintenanceRecordActionOutcome;

    /// <summary>400 Bad Request — FluentValidation failures keyed by property name.</summary>
    public sealed record ValidationFailed(IReadOnlyDictionary<string, string[]> Errors) : MaintenanceRecordActionOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : MaintenanceRecordActionOutcome;

    /// <summary>404 Not Found — the target vehicle does not exist or is outside the caller's organization.</summary>
    public sealed record NotFound : MaintenanceRecordActionOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : MaintenanceRecordActionOutcome;
}
