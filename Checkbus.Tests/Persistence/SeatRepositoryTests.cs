using Checkbus.ApiService.Domain.Entities.Seats;
using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Infrastructure.Implementations.Repositories;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Tests.Persistence;

/// <summary>
/// Proves <see cref="SeatRepository.EnsureSeatsForVehicleAsync"/> lazily, idempotently
/// backfills <c>Seat</c> rows for a <c>Vehicle</c> — nothing in this codebase creates them
/// at registration time yet (odd/tasks/reservas-publicas.md R3's resolved gap). Same
/// SQLite-in-memory setup as <see cref="ReservationPersistenceTests"/>.
/// </summary>
public class SeatRepositoryTests
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

    private static Organization CreateOrganization() => new()
    {
        Id = Guid.NewGuid(),
        CUIT = "20-12345678-9",
        Name = "Checkbus Demo",
        Slug = "checkbus-demo-" + Guid.NewGuid(),
        LogoUrl = "",
        IsActive = true
    };

    private static Vehicle CreateVehicle(Guid organizationId) => new()
    {
        Id = Guid.NewGuid(),
        Brand = "Mercedes-Benz",
        Model = "O500",
        Year = 2020,
        Patent = "AB123CD",
        Capacity = 45,
        Mileage = 1000,
        Status = VehicleStatus.Activo,
        OrganizationId = organizationId,
        OwnerType = VehicleOwnerType.Organizacion
    };

    [Fact]
    public async Task EnsureSeatsForVehicleAsync_WhenNoSeatsExist_CreatesExactlyCapacitySeats()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization();
        db.Organizations.Add(organization);
        var vehicle = CreateVehicle(organization.Id);
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new SeatRepository(db);
        await repository.EnsureSeatsForVehicleAsync(vehicle.Id, vehicle.Capacity, cancellationToken);

        var seats = await db.Seats.Where(s => s.VehicleId == vehicle.Id).ToListAsync(cancellationToken);
        Assert.Equal(vehicle.Capacity, seats.Count);
        Assert.Equal(Enumerable.Range(1, vehicle.Capacity), seats.Select(s => s.Number).OrderBy(n => n));
    }

    [Fact]
    public async Task EnsureSeatsForVehicleAsync_CalledTwice_IsIdempotent_NoDuplicatesNoException()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization();
        db.Organizations.Add(organization);
        var vehicle = CreateVehicle(organization.Id);
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new SeatRepository(db);
        await repository.EnsureSeatsForVehicleAsync(vehicle.Id, vehicle.Capacity, cancellationToken);

        // Second call must be a pure no-op read — no duplicate inserts, no unique-index
        // violation, still exactly Capacity seats.
        await repository.EnsureSeatsForVehicleAsync(vehicle.Id, vehicle.Capacity, cancellationToken);

        var seats = await db.Seats.Where(s => s.VehicleId == vehicle.Id).ToListAsync(cancellationToken);
        Assert.Equal(vehicle.Capacity, seats.Count);
        Assert.Equal(seats.Select(s => s.Number).Distinct().Count(), seats.Count);
    }

    [Fact]
    public async Task EnsureSeatsForVehicleAsync_WhenSomeSeatsAlreadyExist_OnlyBackfillsMissingNumbers()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization();
        db.Organizations.Add(organization);
        var vehicle = CreateVehicle(organization.Id);
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync(cancellationToken);

        db.Seats.Add(new Seat { Id = Guid.NewGuid(), VehicleId = vehicle.Id, Number = 1 });
        db.Seats.Add(new Seat { Id = Guid.NewGuid(), VehicleId = vehicle.Id, Number = 2 });
        await db.SaveChangesAsync(cancellationToken);

        var repository = new SeatRepository(db);
        await repository.EnsureSeatsForVehicleAsync(vehicle.Id, vehicle.Capacity, cancellationToken);

        var seats = await db.Seats.Where(s => s.VehicleId == vehicle.Id).ToListAsync(cancellationToken);
        Assert.Equal(vehicle.Capacity, seats.Count);
        Assert.Equal(Enumerable.Range(1, vehicle.Capacity), seats.Select(s => s.Number).OrderBy(n => n));
    }

    [Fact]
    public async Task GetByVehicleIdAsync_ReturnsAllSeatsForThatVehicle()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization();
        db.Organizations.Add(organization);
        var vehicle = CreateVehicle(organization.Id);
        var otherVehicle = CreateVehicle(organization.Id);
        otherVehicle.Patent = "ZZ999ZZ";
        db.Vehicles.AddRange(vehicle, otherVehicle);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new SeatRepository(db);
        await repository.EnsureSeatsForVehicleAsync(vehicle.Id, 5, cancellationToken);
        await repository.EnsureSeatsForVehicleAsync(otherVehicle.Id, 3, cancellationToken);

        var seats = await repository.GetByVehicleIdAsync(vehicle.Id, cancellationToken);
        Assert.Equal(5, seats.Count);
        Assert.All(seats, s => Assert.Equal(vehicle.Id, s.VehicleId));
    }
}
