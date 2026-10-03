using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Infrastructure.Implementations.Repositories;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Tests.Persistence;

public class VehicleDocumentRepositoryTests
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

    private static VehicleDocument CreateDocument(
        Guid vehicleId,
        VehicleDocumentType type,
        VehicleDocumentStatus status = VehicleDocumentStatus.Pendiente,
        DateOnly? expirationDate = null) => new()
    {
        Id = Guid.NewGuid(),
        VehicleId = vehicleId,
        Type = type,
        Status = status,
        ExpirationDate = expirationDate,
        DocumentPresent = false
    };

    [Fact]
    public async Task AddAsync_PersistsDocument_AndGetByVehicleIdAsync_RetrievesIt()
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

        var repository = new VehicleDocumentRepository(db);
        await repository.AddAsync(CreateDocument(vehicle.Id, VehicleDocumentType.Seguro), cancellationToken);

        var documents = await repository.GetByVehicleIdAsync(vehicle.Id, cancellationToken);

        Assert.Single(documents);
        Assert.Equal(VehicleDocumentType.Seguro, documents[0].Type);
    }

    [Fact]
    public async Task AddRangeAsync_PersistsAllRows()
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

        var repository = new VehicleDocumentRepository(db);
        var documents = new[]
        {
            CreateDocument(vehicle.Id, VehicleDocumentType.Seguro),
            CreateDocument(vehicle.Id, VehicleDocumentType.RTO_VTV)
        };

        await repository.AddRangeAsync(documents, cancellationToken);

        Assert.Equal(2, await db.VehicleDocuments.CountAsync(cancellationToken));
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

        var repository = new VehicleDocumentRepository(db);
        var document = CreateDocument(vehicle.Id, VehicleDocumentType.Seguro);
        await repository.AddAsync(document, cancellationToken);

        document.Status = VehicleDocumentStatus.Apto;
        await repository.UpdateAsync(document, cancellationToken);

        var reloaded = await db.VehicleDocuments.FirstAsync(d => d.Id == document.Id, cancellationToken);
        Assert.Equal(VehicleDocumentStatus.Apto, reloaded.Status);
    }

    [Fact]
    public async Task UniqueIndex_DuplicateVehicleIdAndType_IsRejectedByUniqueIndex()
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

        var repository = new VehicleDocumentRepository(db);
        await repository.AddAsync(CreateDocument(vehicle.Id, VehicleDocumentType.Seguro), cancellationToken);

        await Assert.ThrowsAsync<DbUpdateException>(() => repository.AddAsync(
            CreateDocument(vehicle.Id, VehicleDocumentType.Seguro), cancellationToken));
    }

    [Fact]
    public async Task GetExpiringOrExpiredCountByOrganizationAsync_CountsOnlySameOrganizationRowsWithinThreshold()
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

        var today = new DateOnly(2026, 10, 2);
        var threshold = today.AddDays(30);

        db.VehicleDocuments.AddRange(
            // Org A, already expired -> counted.
            CreateDocument(vehicleA.Id, VehicleDocumentType.Seguro, expirationDate: today.AddDays(-5)),
            // Org A, expires exactly at threshold -> counted.
            CreateDocument(vehicleA.Id, VehicleDocumentType.RTO_VTV, expirationDate: threshold),
            // Org B, same expiring window -> excluded (different organization).
            CreateDocument(vehicleB.Id, VehicleDocumentType.Seguro, expirationDate: today.AddDays(-5)));
        await db.SaveChangesAsync(cancellationToken);

        // Org A, additional vehicle to cover "no expiration date" and "beyond threshold"
        // exclusions without colliding with the unique (VehicleId, Type) index above.
        var vehicleA2 = CreateVehicle(organizationA.Id, "AB333CD");
        db.Vehicles.Add(vehicleA2);
        db.VehicleDocuments.AddRange(
            CreateDocument(vehicleA2.Id, VehicleDocumentType.Seguro, expirationDate: null),
            CreateDocument(vehicleA2.Id, VehicleDocumentType.RTO_VTV, expirationDate: threshold.AddDays(1)));
        await db.SaveChangesAsync(cancellationToken);

        var repository = new VehicleDocumentRepository(db);

        var count = await repository.GetExpiringOrExpiredCountByOrganizationAsync(organizationA.Id, threshold, cancellationToken);

        Assert.Equal(2, count);
    }
}
