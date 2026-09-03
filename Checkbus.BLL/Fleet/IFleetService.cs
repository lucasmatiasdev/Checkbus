using Checkbus.BEL.Fleet.Enums;

namespace Checkbus.BLL.Fleet;

/// <summary>
/// S3.7 — minimal Fleet-module use cases wired to UI: driver onboarding with
/// optional document upload, vehicle onboarding with documentation and an
/// initial diagnostic, and maintenance record entry.
/// </summary>
public interface IFleetService
{
    /// <summary>Creates a Driver with its 1—1 License and, optionally, one Attachment (e.g. license scan).</summary>
    Task<int> CreateDriverAsync(
        int organizationId,
        string fullName,
        string dni,
        LicenseCategory licenseCategory,
        DateTime licenseExpiryDate,
        FileUpload? licenseDocument,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a Vehicle with one initial VehicleDocumentation (optionally with an Attachment) and one
    /// initial VehicleDiagnostic. <see cref="Vehicle.CurrentDiagnosticState"/> is set to the worst
    /// (highest-value) rating among <paramref name="componentRatings"/>.
    /// </summary>
    Task<int> CreateVehicleAsync(
        int organizationId,
        string plate,
        int capacity,
        VehicleDocumentationType documentationType,
        string documentNumber,
        VehicleDocumentationState documentationState,
        FileUpload? documentationAttachment,
        int diagnosticMileage,
        IReadOnlyList<ComponentRatingInput> componentRatings,
        CancellationToken cancellationToken = default);

    /// <summary>Records a MaintenanceRecord event for an existing Vehicle.</summary>
    Task<int> CreateMaintenanceRecordAsync(
        int vehicleId,
        DateTime eventDate,
        string description,
        string affectedComponents,
        MaintenanceStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>Vehicles for the given organization, for use in pickers (e.g. maintenance-record entry).</summary>
    Task<IReadOnlyList<VehicleSummary>> GetVehiclesAsync(int organizationId, CancellationToken cancellationToken = default);
}
