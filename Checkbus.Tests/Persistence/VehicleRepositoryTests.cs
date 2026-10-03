using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Infrastructure.Implementations.Repositories;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Tests.Persistence;

public class VehicleRepositoryTests
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

    private static Vehicle CreateVehicle(
        Guid organizationId,
        string patent,
        VehicleOwnerType ownerType = VehicleOwnerType.Organizacion) => new()
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
        OwnerType = ownerType
    };

    [Fact]
    public async Task AddAsync_PersistsVehicle_AndGetByIdAsync_RetrievesIt()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization("checkbus-demo");
        db.Organizations.Add(organization);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new VehicleRepository(db);
        var vehicle = CreateVehicle(organization.Id, "AB123CD");

        await repository.AddAsync(vehicle, cancellationToken);

        var retrieved = await repository.GetByIdAsync(vehicle.Id, cancellationToken);
        Assert.NotNull(retrieved);
        Assert.Equal("AB123CD", retrieved!.Patent);
        Assert.Equal(organization.Id, retrieved.OrganizationId);
    }

    [Fact]
    public async Task AddAsync_DuplicatePatent_IsRejectedByUniqueIndex()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization("checkbus-demo");
        db.Organizations.Add(organization);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new VehicleRepository(db);
        await repository.AddAsync(CreateVehicle(organization.Id, "AB123CD"), cancellationToken);

        await Assert.ThrowsAsync<DbUpdateException>(() => repository.AddAsync(
            CreateVehicle(organization.Id, "AB123CD"), cancellationToken));
    }

    [Fact]
    public async Task PatentExistsAsync_ExistingPatent_ReturnsTrue_NonExisting_ReturnsFalse()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization("checkbus-demo");
        db.Organizations.Add(organization);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new VehicleRepository(db);
        await repository.AddAsync(CreateVehicle(organization.Id, "AB123CD"), cancellationToken);

        Assert.True(await repository.PatentExistsAsync("AB123CD", cancellationToken));
        Assert.False(await repository.PatentExistsAsync("ZZ999ZZ", cancellationToken));
    }

    [Fact]
    public async Task GetAllByOrganizationAsync_ReturnsOnlyVehiclesFromThatOrganization()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organizationA = CreateOrganization("org-a");
        var organizationB = CreateOrganization("org-b");
        db.Organizations.AddRange(organizationA, organizationB);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new VehicleRepository(db);
        await repository.AddAsync(CreateVehicle(organizationA.Id, "AB111CD"), cancellationToken);
        await repository.AddAsync(CreateVehicle(organizationA.Id, "AB222CD"), cancellationToken);
        await repository.AddAsync(CreateVehicle(organizationB.Id, "AB333CD"), cancellationToken);

        var vehicles = await repository.GetAllByOrganizationAsync(organizationA.Id, cancellationToken);

        Assert.Equal(2, vehicles.Count);
        Assert.All(vehicles, v => Assert.Equal(organizationA.Id, v.OrganizationId));
        Assert.DoesNotContain(vehicles, v => v.Patent == "AB333CD");
    }
}
