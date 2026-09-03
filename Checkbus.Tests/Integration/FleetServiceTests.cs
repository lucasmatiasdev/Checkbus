using System.Text;
using Checkbus.BEL.Fleet.Enums;
using Checkbus.BLL.Fleet;
using Checkbus.BLL.Storage;
using Organization = Checkbus.BEL.Organization.Organization;

namespace Checkbus.Tests.Integration;

/// <summary>
/// S3.7 — <see cref="IFleetService"/>: driver onboarding with a license
/// document, vehicle onboarding with documentation and an initial
/// diagnostic, and maintenance record entry.
/// </summary>
public class FleetServiceTests : IDisposable
{
    private readonly string _fileStorageBasePath = Path.Combine(Path.GetTempPath(), "checkbus-tests-" + Guid.NewGuid());

    [Fact]
    public async Task CreateDriverAsync_Creates_The_Driver_License_And_Attachment()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();
        int orgId;
        using (var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true }))
        {
            systemContext.Database.EnsureCreated();
            var org = new Organization { Name = "Org A", Cuit = "30-30303030-1" };
            systemContext.Organizations.Add(org);
            systemContext.SaveChanges();
            orgId = org.Id;
        }

        var sut = CreateSut(connection, orgId);
        var licenseDocument = new FileUpload("license.pdf", "pdf", new MemoryStream(Encoding.UTF8.GetBytes("scan")));

        var driverId = await sut.CreateDriverAsync(
            orgId, "Juan Perez", "30111222", LicenseCategory.D, DateTime.UtcNow.AddYears(2), licenseDocument);

        using var verifyContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { CurrentOrganizationId = orgId });
        var driver = verifyContext.Drivers.Single(d => d.Id == driverId);
        Assert.Equal("Juan Perez", driver.FullName);
        var license = verifyContext.Licenses.Single(l => l.DriverId == driverId);
        Assert.Equal(LicenseCategory.D, license.Category);
        var attachment = verifyContext.Attachments.Single(a => a.DriverId == driverId);
        Assert.Equal($"{orgId}/drivers/{driverId}/license.pdf", attachment.StoragePath);
        Assert.True(File.Exists(Path.Combine(_fileStorageBasePath, orgId.ToString(), "drivers", driverId.ToString(), "license.pdf")));
    }

    [Fact]
    public async Task CreateVehicleAsync_Creates_The_Vehicle_Documentation_And_Diagnostic_With_Worst_Rating()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();
        int orgId;
        using (var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true }))
        {
            systemContext.Database.EnsureCreated();
            var org = new Organization { Name = "Org A", Cuit = "30-30303030-2" };
            systemContext.Organizations.Add(org);
            systemContext.SaveChanges();
            orgId = org.Id;
        }

        var sut = CreateSut(connection, orgId);
        var componentRatings = new List<ComponentRatingInput>
        {
            new(VehicleComponent.Motor, ComponentRating.Optimo),
            new(VehicleComponent.Frenos, ComponentRating.Critico),
        };

        var vehicleId = await sut.CreateVehicleAsync(
            orgId, "AB123CD", 40,
            VehicleDocumentationType.Vtv, "VTV-001", VehicleDocumentationState.Vigente,
            documentationAttachment: null,
            diagnosticMileage: 12000,
            componentRatings);

        using var verifyContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { CurrentOrganizationId = orgId });
        var vehicle = verifyContext.Vehicles.Single(v => v.Id == vehicleId);
        Assert.Equal(ComponentRating.Critico, vehicle.CurrentDiagnosticState);
        var documentation = verifyContext.VehicleDocumentations.Single(d => d.VehicleId == vehicleId);
        Assert.Equal("VTV-001", documentation.DocumentNumber);
        var diagnostic = verifyContext.VehicleDiagnostics.Single(d => d.VehicleId == vehicleId);
        Assert.Equal(ComponentRating.Critico, diagnostic.OverallState);
        Assert.Equal(2, verifyContext.ComponentDiagnostics.Count(c => c.VehicleDiagnosticId == diagnostic.Id));
    }

    [Fact]
    public async Task CreateMaintenanceRecordAsync_Creates_A_MaintenanceRecord_For_The_Vehicle()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();
        int orgId, vehicleId;
        using (var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true }))
        {
            systemContext.Database.EnsureCreated();
            var org = new Organization { Name = "Org A", Cuit = "30-30303030-3" };
            systemContext.Organizations.Add(org);
            systemContext.SaveChanges();
            orgId = org.Id;

            var vehicle = new Checkbus.BEL.Fleet.Vehicle { OrganizationId = orgId, Plate = "ZZ999YY", Capacity = 30 };
            systemContext.Vehicles.Add(vehicle);
            systemContext.SaveChanges();
            vehicleId = vehicle.Id;
        }

        var sut = CreateSut(connection, orgId);

        var recordId = await sut.CreateMaintenanceRecordAsync(
            vehicleId, DateTime.UtcNow, "Cambio de aceite", "Motor", MaintenanceStatus.Completado);

        using var verifyContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { CurrentOrganizationId = orgId });
        var record = verifyContext.MaintenanceRecords.Single(m => m.Id == recordId);
        Assert.Equal("Cambio de aceite", record.Description);
        Assert.Equal(MaintenanceStatus.Completado, record.Status);
    }

    [Fact]
    public async Task GetVehiclesAsync_Returns_Only_The_Organizations_Own_Vehicles()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();
        int orgAId, orgBId;
        using (var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true }))
        {
            systemContext.Database.EnsureCreated();
            var orgA = new Organization { Name = "Org A", Cuit = "30-30303030-4" };
            var orgB = new Organization { Name = "Org B", Cuit = "30-30303030-5" };
            systemContext.Organizations.AddRange(orgA, orgB);
            systemContext.SaveChanges();
            orgAId = orgA.Id;
            orgBId = orgB.Id;

            systemContext.Vehicles.Add(new Checkbus.BEL.Fleet.Vehicle { OrganizationId = orgAId, Plate = "AA111AA", Capacity = 10 });
            systemContext.Vehicles.Add(new Checkbus.BEL.Fleet.Vehicle { OrganizationId = orgBId, Plate = "BB222BB", Capacity = 20 });
            systemContext.SaveChanges();
        }

        var sut = CreateSut(connection, orgAId);

        var vehicles = await sut.GetVehiclesAsync(orgAId);

        Assert.Single(vehicles);
        Assert.Equal("AA111AA", vehicles[0].Plate);
    }

    private FleetService CreateSut(Microsoft.Data.Sqlite.SqliteConnection connection, int organizationId)
    {
        var factory = new FakeDbContextFactory(connection, new FakeTenantProvider { CurrentOrganizationId = organizationId });
        var fileStorageService = new LocalDiskFileStorageService(_fileStorageBasePath);
        return new FleetService(factory, fileStorageService);
    }

    public void Dispose()
    {
        if (Directory.Exists(_fileStorageBasePath))
        {
            Directory.Delete(_fileStorageBasePath, recursive: true);
        }
    }
}
