using Checkbus.Web.Contracts;

namespace Checkbus.Web.Services;

/// <summary>
/// Closed outcome of a <c>GET /api/VehicleDiagnostics?maintenanceRecordId=...</c> call, covering
/// every realistic server response so callers can pattern-match on the result without inspecting
/// HTTP status codes directly. Unlike <see cref="MaintenanceRecordsListOutcome"/>, a 404 is a real,
/// distinct case here — a diagnostics list always belongs to exactly one maintenance record, and a
/// missing/out-of-org <c>maintenanceRecordId</c> is reported as not found rather than an empty list.
/// The private constructor confines the hierarchy to the nested cases declared here; a consumer can
/// exhaustively switch over <see cref="Success"/>, <see cref="Forbidden"/>,
/// <see cref="NotFound"/>, and <see cref="TransportError"/>.
/// </summary>
public abstract record VehicleDiagnosticsListOutcome
{
    private VehicleDiagnosticsListOutcome()
    {
    }

    /// <summary>200 OK — the diagnostics for the given maintenance record were fetched successfully.</summary>
    public sealed record Success(IReadOnlyList<VehicleDiagnosticResponse> Diagnostics) : VehicleDiagnosticsListOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : VehicleDiagnosticsListOutcome;

    /// <summary>404 Not Found — the parent maintenance record does not exist or is outside the caller's organization.</summary>
    public sealed record NotFound : VehicleDiagnosticsListOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : VehicleDiagnosticsListOutcome;
}
