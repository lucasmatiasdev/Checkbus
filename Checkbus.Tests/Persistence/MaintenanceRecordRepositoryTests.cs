using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Infrastructure.Implementations.Repositories;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Tests.Persistence;

public class MaintenanceRecordRepositoryTests
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

    private static MaintenanceRecord CreateRecord(
        Guid vehicleId,
        MaintenanceType type = MaintenanceType.Preventivo,
        MaintenanceStatus status = MaintenanceStatus.Programado) => new()
    {
        Id = Guid.NewGuid(),
        VehicleId = vehicleId,
        Type = type,
        ScheduledDate = new DateOnly(2026, 10, 10),
        Status = status,
        Description = "Cambio de aceite y filtros"
    };

    [Fact]
    public async Task AddAsync_PersistsRecord_AndGetByIdAsync_RetrievesIt()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization("checkbus-demo");
        db.Organizations.Add(organization);
        var vehicle = CreateVehicle(organization.Id, "AB123CD");
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new MaintenanceRecordRepository(db);
        var record = CreateRecord(vehicle.Id);
        await repository.AddAsync(record, cancellationToken);

        var reloaded = await repository.GetByIdAsync(record.Id, cancellationToken);

        Assert.NotNull(reloaded);
        Assert.Equal(MaintenanceType.Preventivo, reloaded!.Type);
        Assert.Equal(vehicle.Id, reloaded.VehicleId);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new MaintenanceRecordRepository(db);

        var result = await repository.GetByIdAsync(Guid.NewGuid(), cancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_PersistsStatusChange()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization("checkbus-demo");
        db.Organizations.Add(organization);
        var vehicle = CreateVehicle(organization.Id, "AB123CD");
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new MaintenanceRecordRepository(db);
        var record = CreateRecord(vehicle.Id);
        await repository.AddAsync(record, cancellationToken);

        record.Status = MaintenanceStatus.EnProceso;
        record.StartDate = new DateOnly(2026, 10, 11);
        await repository.UpdateAsync(record, cancellationToken);

        var reloaded = await db.Set<MaintenanceRecord>().FirstAsync(r => r.Id == record.Id, cancellationToken);
        Assert.Equal(MaintenanceStatus.EnProceso, reloaded.Status);
        Assert.Equal(new DateOnly(2026, 10, 11), reloaded.StartDate);
    }

    [Fact]
    public async Task GetByVehicleIdsAsync_ReturnsOnlyRecordsForGivenVehicleIds()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organizationA = CreateOrganization("org-a");
        var organizationB = CreateOrganization("org-b");
        db.Organizations.AddRange(organizationA, organizationB);

        var vehicleA = CreateVehicle(organizationA.Id, "AB111CD");
        var vehicleB = CreateVehicle(organizationB.Id, "AB222CD");
        db.Vehicles.AddRange(vehicleA, vehicleB);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new MaintenanceRecordRepository(db);
        await repository.AddAsync(CreateRecord(vehicleA.Id), cancellationToken);
        await repository.AddAsync(CreateRecord(vehicleA.Id, MaintenanceType.Reactivo), cancellationToken);
        await repository.AddAsync(CreateRecord(vehicleB.Id), cancellationToken);

        var records = await repository.GetByVehicleIdsAsync(new[] { vehicleA.Id }, cancellationToken);

        Assert.Equal(2, records.Count);
        Assert.All(records, r => Assert.Equal(vehicleA.Id, r.VehicleId));
    }
}
