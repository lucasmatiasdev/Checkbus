namespace Checkbus.Web.Services;

/// <summary>
/// Closed outcome of a <c>PUT /api/MaintenanceRecords/{id}</c> call, covering every realistic
/// server response so callers can pattern-match on the result without inspecting HTTP status codes
/// directly. The endpoint returns <c>204 No Content</c> on success (no response body), unlike
/// <see cref="MaintenanceRecordActionOutcome"/>'s create case — mirrors
/// <see cref="VehicleDocumentActionOutcome"/>'s no-body-success shape. The private constructor
/// confines the hierarchy to the nested cases declared here; a consumer can exhaustively switch
/// over <see cref="Success"/>, <see cref="ValidationFailed"/>, <see cref="Forbidden"/>,
/// <see cref="NotFound"/>, and <see cref="TransportError"/>.
/// </summary>
public abstract record MaintenanceRecordUpdateOutcome
{
    private MaintenanceRecordUpdateOutcome()
    {
    }

    /// <summary>204 No Content — the maintenance record was updated successfully.</summary>
    public sealed record Success : MaintenanceRecordUpdateOutcome;

    /// <summary>400 Bad Request — FluentValidation failures keyed by property name.</summary>
    public sealed record ValidationFailed(IReadOnlyDictionary<string, string[]> Errors) : MaintenanceRecordUpdateOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : MaintenanceRecordUpdateOutcome;

    /// <summary>404 Not Found — the record does not exist or is outside the caller's organization.</summary>
    public sealed record NotFound : MaintenanceRecordUpdateOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : MaintenanceRecordUpdateOutcome;
}
