using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Infrastructure.Implementations.Repositories;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Tests.Persistence;

public class VehicleDiagnosticRepositoryTests
{
    private static SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=True");
        connection.Open();
        return connection;
    }

    private static CheckbusDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<CheckbusDbContext>()
            .UseSqlite(connection)
            .Options;
        return new CheckbusDbContext(options);
    }

    private static Organization CreateOrganization(string slug) => new()
    {
        Id = Guid.NewGuid(),
        CUIT = "20-12345678-9",
        Name = "Checkbus Demo",
        Slug = slug,
        LogoUrl = "",
        IsActive = true
    };

    private static Vehicle CreateVehicle(Guid organizationId, string patent) => new()
    {
        Id = Guid.NewGuid(),
        Brand = "Mercedes-Benz",
        Model = "O500",
        Year = 2020,
        Patent = patent,
        Capacity = 45,
        Mileage = 1000,
        Status = VehicleStatus.Activo,
        OrganizationId = organizationId,
        OwnerType = VehicleOwnerType.Organizacion
    };

    private static MaintenanceRecord CreateMaintenanceRecord(Guid vehicleId) => new()
    {
        Id = Guid.NewGuid(),
        VehicleId = vehicleId,
        Type = MaintenanceType.Preventivo,
        ScheduledDate = new DateOnly(2026, 10, 10),
        Description = "Service general"
    };

    private static VehicleDiagnostic CreateDiagnostic(Guid vehicleId, Guid maintenanceRecordId) => new()
    {
        Id = Guid.NewGuid(),
        VehicleId = vehicleId,
        MaintenanceRecordId = maintenanceRecordId,
        DiagnosedAt = new DateOnly(2026, 10, 10),
        Notes = "Revision general"
    };

    private static ComponentDiagnostic CreateComponent(
        Guid diagnosticId,
        VehicleComponent component,
        ComponentCondition condition) => new()
    {
        Id = Guid.NewGuid(),
        VehicleDiagnosticId = diagnosticId,
        Component = component,
        Condition = condition
    };

    [Fact]
    public async Task AddAsync_PersistsDiagnosticAndComponents()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization("checkbus-demo");
        db.Organizations.Add(organization);
        var vehicle = CreateVehicle(organization.Id, "AB123CD");
        db.Vehicles.Add(vehicle);
        var maintenanceRecord = CreateMaintenanceRecord(vehicle.Id);
        db.Set<MaintenanceRecord>().Add(maintenanceRecord);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new VehicleDiagnosticRepository(db);
        var diagnostic = CreateDiagnostic(vehicle.Id, maintenanceRecord.Id);
        var components = new[]
        {
            CreateComponent(diagnostic.Id, VehicleComponent.Motor, ComponentCondition.Bueno),
            CreateComponent(diagnostic.Id, VehicleComponent.Frenos, ComponentCondition.Desgastado)
        };

        await repository.AddAsync(diagnostic, components, cancellationToken);

        var persistedComponents = await db.Set<ComponentDiagnostic>()
            .Where(c => c.VehicleDiagnosticId == diagnostic.Id)
            .ToListAsync(cancellationToken);

        Assert.Equal(1, await db.Set<VehicleDiagnostic>().CountAsync(cancellationToken));
        Assert.Equal(2, persistedComponents.Count);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new VehicleDiagnosticRepository(db);

        var result = await repository.GetByIdAsync(Guid.NewGuid(), cancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByMaintenanceRecordIdAsync_ReturnsDiagnosticsForThatRecord_WithComponentsQueryableSeparately()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization("checkbus-demo");
        db.Organizations.Add(organization);
        var vehicle = CreateVehicle(organization.Id, "AB123CD");
        db.Vehicles.Add(vehicle);
        var maintenanceRecordA = CreateMaintenanceRecord(vehicle.Id);
        var maintenanceRecordB = CreateMaintenanceRecord(vehicle.Id);
        db.Set<MaintenanceRecord>().AddRange(maintenanceRecordA, maintenanceRecordB);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new VehicleDiagnosticRepository(db);
        var diagnosticA = CreateDiagnostic(vehicle.Id, maintenanceRecordA.Id);
        await repository.AddAsync(diagnosticA, new[] { CreateComponent(diagnosticA.Id, VehicleComponent.Motor, ComponentCondition.Bueno) }, cancellationToken);

        var diagnosticB = CreateDiagnostic(vehicle.Id, maintenanceRecordB.Id);
        await repository.AddAsync(diagnosticB, new[] { CreateComponent(diagnosticB.Id, VehicleComponent.Frenos, ComponentCondition.Fallado) }, cancellationToken);

        var diagnosticsForA = await repository.GetByMaintenanceRecordIdAsync(maintenanceRecordA.Id, cancellationToken);

        Assert.Single(diagnosticsForA);
        Assert.Equal(diagnosticA.Id, diagnosticsForA[0].Id);

        var componentsForA = await db.Set<ComponentDiagnostic>()
            .Where(c => c.VehicleDiagnosticId == diagnosticA.Id)
            .ToListAsync(cancellationToken);
        Assert.Single(componentsForA);
        Assert.Equal(VehicleComponent.Motor, componentsForA[0].Component);
    }
}
