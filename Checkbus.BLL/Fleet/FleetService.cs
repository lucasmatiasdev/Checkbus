using Checkbus.BEL.Fleet;
using Checkbus.BEL.Fleet.Enums;
using Checkbus.BLL.Storage;
using Checkbus.DAL.Context;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.BLL.Fleet;

/// <summary>Default <see cref="IFleetService"/>.</summary>
public class FleetService : IFleetService
{
    private readonly IDbContextFactory<CheckbusDbContext> _contextFactory;
    private readonly IFileStorageService _fileStorageService;

    public FleetService(IDbContextFactory<CheckbusDbContext> contextFactory, IFileStorageService fileStorageService)
    {
        _contextFactory = contextFactory;
        _fileStorageService = fileStorageService;
    }

    public async Task<int> CreateDriverAsync(
        int organizationId,
        string fullName,
        string dni,
        LicenseCategory licenseCategory,
        DateTime licenseExpiryDate,
        FileUpload? licenseDocument,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var driver = new Driver
        {
            OrganizationId = organizationId,
            FullName = fullName,
            Dni = dni,
            License = new License { Category = licenseCategory, ExpiryDate = licenseExpiryDate },
        };
        context.Drivers.Add(driver);
        await context.SaveChangesAsync(cancellationToken);

        if (licenseDocument is not null)
        {
            await AttachFileAsync(
                context, organizationId, $"drivers/{driver.Id}", licenseDocument,
                attachment => attachment.DriverId = driver.Id, cancellationToken);
        }

        return driver.Id;
    }

    public async Task<int> CreateVehicleAsync(
        int organizationId,
        string plate,
        int capacity,
        VehicleDocumentationType documentationType,
        string documentNumber,
        VehicleDocumentationState documentationState,
        FileUpload? documentationAttachment,
        int diagnosticMileage,
        IReadOnlyList<ComponentRatingInput> componentRatings,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var vehicle = new Vehicle { OrganizationId = organizationId, Plate = plate, Capacity = capacity };
        context.Vehicles.Add(vehicle);
        await context.SaveChangesAsync(cancellationToken);

        var documentation = new VehicleDocumentation
        {
            VehicleId = vehicle.Id,
            Type = documentationType,
            DocumentNumber = documentNumber,
            State = documentationState,
        };
        context.VehicleDocumentations.Add(documentation);
        await context.SaveChangesAsync(cancellationToken);

        if (documentationAttachment is not null)
        {
            await AttachFileAsync(
                context, organizationId, $"vehicles/{vehicle.Id}/documentation/{documentation.Id}",
                documentationAttachment, attachment => attachment.VehicleDocumentationId = documentation.Id, cancellationToken);
        }

        // Vehicle.CurrentDiagnosticState is a denormalized "worst component wins"
        // value (PR3a comment) — recomputing/enforcing it beyond this initial
        // onboarding diagnostic (e.g. blocking trip assignment while Critico) is
        // deferred to a later change.
        var overallState = componentRatings.Count > 0
            ? componentRatings.Max(r => r.Rating)
            : ComponentRating.Optimo;

        context.VehicleDiagnostics.Add(new VehicleDiagnostic
        {
            VehicleId = vehicle.Id,
            Mileage = diagnosticMileage,
            OverallState = overallState,
            RecordedAt = DateTime.UtcNow,
            Components = componentRatings
                .Select(r => new ComponentDiagnostic { Component = r.Component, Rating = r.Rating })
                .ToList(),
        });

        vehicle.CurrentDiagnosticState = overallState;
        await context.SaveChangesAsync(cancellationToken);

        return vehicle.Id;
    }

    public async Task<int> CreateMaintenanceRecordAsync(
        int vehicleId,
        DateTime eventDate,
        string description,
        string affectedComponents,
        MaintenanceStatus status,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var record = new MaintenanceRecord
        {
            VehicleId = vehicleId,
            EventDate = eventDate,
            Description = description,
            AffectedComponents = affectedComponents,
            Status = status,
        };
        context.MaintenanceRecords.Add(record);
        await context.SaveChangesAsync(cancellationToken);

        return record.Id;
    }

    public async Task<IReadOnlyList<VehicleSummary>> GetVehiclesAsync(int organizationId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Vehicles
            .Where(v => v.OrganizationId == organizationId)
            .OrderBy(v => v.Plate)
            .Select(v => new VehicleSummary(v.Id, v.Plate, v.Capacity, v.CurrentDiagnosticState))
            .ToListAsync(cancellationToken);
    }

    private async Task AttachFileAsync(
        CheckbusDbContext context,
        int organizationId,
        string ownerRelativeFolder,
        FileUpload file,
        Action<Attachment> setOwner,
        CancellationToken cancellationToken)
    {
        var sizeBytes = file.Content.Length;
        var storagePath = await _fileStorageService.SaveAsync(
            organizationId, ownerRelativeFolder, file.FileName, file.Content, cancellationToken);

        var attachment = new Attachment
        {
            StoragePath = storagePath,
            FileName = file.FileName,
            Format = file.Format,
            SizeBytes = sizeBytes,
            UploadedAt = DateTime.UtcNow,
        };
        setOwner(attachment);

        context.Attachments.Add(attachment);
        await context.SaveChangesAsync(cancellationToken);
    }
}
