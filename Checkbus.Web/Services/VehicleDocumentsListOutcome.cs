using Checkbus.Web.Contracts;

namespace Checkbus.Web.Services;

/// <summary>
/// Closed outcome of a <c>GET /api/VehicleDocuments/{vehicleId}</c> call, covering every realistic
/// server response so callers can pattern-match on the result without inspecting HTTP status codes
/// directly. Mirrors <see cref="DriverRequirementsListOutcome"/>'s shape — 404 is a real, distinct
/// case here (the vehicle doesn't exist or is out-of-org), never collapsed into
/// <see cref="Forbidden"/>. The private constructor confines the hierarchy to the nested cases
/// declared here; a consumer can exhaustively switch over <see cref="Success"/>,
/// <see cref="Forbidden"/>, <see cref="NotFound"/>, and <see cref="TransportError"/>.
/// </summary>
public abstract record VehicleDocumentsListOutcome
{
    private VehicleDocumentsListOutcome()
    {
    }

    /// <summary>200 OK — the vehicle's documents were fetched successfully.</summary>
    public sealed record Success(IReadOnlyList<VehicleDocumentResponse> Documents) : VehicleDocumentsListOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : VehicleDocumentsListOutcome;

    /// <summary>404 Not Found — the vehicle does not exist or is outside the caller's organization.</summary>
    public sealed record NotFound : VehicleDocumentsListOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : VehicleDocumentsListOutcome;
}
