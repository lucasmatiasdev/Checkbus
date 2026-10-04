using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Infrastructure.Implementations.Repositories;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Tests.Persistence;

public class TripRepositoryTests
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

    private static User CreateDriver(Guid organizationId, string documentNumber) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Juan",
        Surname = "Perez",
        Email = $"{documentNumber}@checkbus.test",
        PasswordHash = "hash",
        DocumentNumber = documentNumber,
        Role = Role.Chofer,
        OrganizationId = organizationId
    };

    private static Location CreateLocation(string placeId) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Estadio Monumental",
        Address = "Av. Pres. Figueroa Alcorta 7597",
        PlaceId = placeId,
        Latitude = -34.5453,
        Longitude = -58.4498
    };

    private static Event CreateEvent(Guid locationId) => new()
    {
        Id = Guid.NewGuid(),
        Name = "River vs Boca",
        Type = EventType.Partido,
        Date = new DateTime(2026, 11, 1, 20, 0, 0, DateTimeKind.Utc),
        LocationId = locationId
    };

    private static Trip CreateTrip(
        Guid organizationId, Guid driverId, Guid vehicleId, Guid eventId,
        DateTime fechaSalida, DateTime fechaLlegada) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = organizationId,
        DriverId = driverId,
        VehicleId = vehicleId,
        EventId = eventId,
        DepartureDate = fechaSalida,
        ArrivalDate = fechaLlegada,
        Capacity = 45,
        AvailableSeats = 45,
        Price = 1500m,
        Status = TripStatus.Programado
    };

    private static Route CreateRoute(Guid tripId) => new()
    {
        Id = Guid.NewGuid(),
        TripId = tripId,
        EstimatedDuration = TimeSpan.FromHours(2),
        DistanceKm = 120.5m
    };

    private static Stop CreateStop(Guid routeId, Guid locationId, int orden, StopType tipo) => new()
    {
        Id = Guid.NewGuid(),
        RouteId = routeId,
        LocationId = locationId,
        Order = orden,
        Type = tipo
    };

    private async Task<(CheckbusDbContext Db, SqliteConnection Connection, Organization Organization, Vehicle Vehicle, User Driver, Event Event)> SeedAsync(CancellationToken cancellationToken)
    {
        var connection = CreateOpenConnection();
        var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization("checkbus-demo");
        var vehicle = CreateVehicle(organization.Id, "AB123CD");
        var driver = CreateDriver(organization.Id, "30111222");
        var location = CreateLocation("ChIJ-trip");
        db.Organizations.Add(organization);
        db.Vehicles.Add(vehicle);
        db.Users.Add(driver);
        db.Locations.Add(location);
        await db.SaveChangesAsync(cancellationToken);

        var @event = CreateEvent(location.Id);
        db.Events.Add(@event);
        await db.SaveChangesAsync(cancellationToken);

        return (db, connection, organization, vehicle, driver, @event);
    }

    [Fact]
    public async Task AddAsync_PersistsTripRouteAndStops()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var (db, connection, organization, vehicle, driver, @event) = await SeedAsync(cancellationToken);
        using var _ = connection;
        using var __ = db;

        var repository = new TripRepository(db);
        var trip = CreateTrip(
            organization.Id, driver.Id, vehicle.Id, @event.Id,
            new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 1, 14, 0, 0, DateTimeKind.Utc));
        var route = CreateRoute(trip.Id);
        var stops = new[]
        {
            CreateStop(route.Id, @event.LocationId, 0, StopType.Origen),
            CreateStop(route.Id, @event.LocationId, 1, StopType.Destino)
        };

        await repository.AddAsync(trip, route, stops, cancellationToken);

        var persistedTrip = await repository.GetByIdAsync(trip.Id, cancellationToken);
        var persistedRoute = await db.Routes.FirstOrDefaultAsync(r => r.TripId == trip.Id, cancellationToken);
        var persistedStops = await db.Stops.Where(s => s.RouteId == route.Id).ToListAsync(cancellationToken);

        Assert.NotNull(persistedTrip);
        Assert.NotNull(persistedRoute);
        Assert.Equal(2, persistedStops.Count);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new TripRepository(db);

        var result = await repository.GetByIdAsync(Guid.NewGuid(), cancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllByOrganizationAsync_ReturnsOnlyThatOrganizationsTrips()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var (db, connection, organization, vehicle, driver, @event) = await SeedAsync(cancellationToken);
        using var _ = connection;
        using var __ = db;

        var otherOrganization = CreateOrganization("other-org");
        db.Organizations.Add(otherOrganization);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new TripRepository(db);
        var tripInOrg = CreateTrip(
            organization.Id, driver.Id, vehicle.Id, @event.Id,
            new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 1, 14, 0, 0, DateTimeKind.Utc));
        var tripInOtherOrg = CreateTrip(
            otherOrganization.Id, driver.Id, vehicle.Id, @event.Id,
            new DateTime(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 2, 14, 0, 0, DateTimeKind.Utc));
        await repository.AddAsync(tripInOrg, CreateRoute(tripInOrg.Id), [], cancellationToken);
        await repository.AddAsync(tripInOtherOrg, CreateRoute(tripInOtherOrg.Id), [], cancellationToken);

        var result = await repository.GetAllByOrganizationAsync(organization.Id, cancellationToken);

        Assert.Single(result);
        Assert.Equal(tripInOrg.Id, result[0].Id);
    }

    [Theory]
    // Fully contained inside the existing trip's range.
    [InlineData("2026-11-01T11:00:00Z", "2026-11-01T13:00:00Z", true)]
    // Starts exactly when the existing trip ends — boundary is inclusive, counts as overlap.
    [InlineData("2026-11-01T14:00:00Z", "2026-11-01T16:00:00Z", true)]
    // Ends exactly when the existing trip starts — boundary is inclusive, counts as overlap.
    [InlineData("2026-11-01T08:00:00Z", "2026-11-01T10:00:00Z", true)]
    // Entirely before the existing trip, no contact at all.
    [InlineData("2026-11-01T06:00:00Z", "2026-11-01T09:00:00Z", false)]
    // Entirely after the existing trip, no contact at all.
    [InlineData("2026-11-01T15:00:00Z", "2026-11-01T17:00:00Z", false)]
    public async Task GetOverlappingByVehicleIdAsync_DetectsOverlapIncludingBoundaries(
        string candidateStart, string candidateEnd, bool expectOverlap)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var (db, connection, organization, vehicle, driver, @event) = await SeedAsync(cancellationToken);
        using var _ = connection;
        using var __ = db;

        var repository = new TripRepository(db);
        var existingTrip = CreateTrip(
            organization.Id, driver.Id, vehicle.Id, @event.Id,
            new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 1, 14, 0, 0, DateTimeKind.Utc));
        await repository.AddAsync(existingTrip, CreateRoute(existingTrip.Id), [], cancellationToken);

        var result = await repository.GetOverlappingByVehicleIdAsync(
            vehicle.Id,
            DateTime.Parse(candidateStart).ToUniversalTime(),
            DateTime.Parse(candidateEnd).ToUniversalTime(),
            cancellationToken);

        Assert.Equal(expectOverlap, result.Any(v => v.Id == existingTrip.Id));
    }

    [Theory]
    [InlineData("2026-11-01T11:00:00Z", "2026-11-01T13:00:00Z", true)]
    [InlineData("2026-11-01T14:00:00Z", "2026-11-01T16:00:00Z", true)]
    [InlineData("2026-11-01T08:00:00Z", "2026-11-01T10:00:00Z", true)]
    [InlineData("2026-11-01T06:00:00Z", "2026-11-01T09:00:00Z", false)]
    [InlineData("2026-11-01T15:00:00Z", "2026-11-01T17:00:00Z", false)]
    public async Task GetOverlappingByDriverIdAsync_DetectsOverlapIncludingBoundaries(
        string candidateStart, string candidateEnd, bool expectOverlap)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var (db, connection, organization, vehicle, driver, @event) = await SeedAsync(cancellationToken);
        using var _ = connection;
        using var __ = db;

        var repository = new TripRepository(db);
        var existingTrip = CreateTrip(
            organization.Id, driver.Id, vehicle.Id, @event.Id,
            new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 1, 14, 0, 0, DateTimeKind.Utc));
        await repository.AddAsync(existingTrip, CreateRoute(existingTrip.Id), [], cancellationToken);

        var result = await repository.GetOverlappingByDriverIdAsync(
            driver.Id,
            DateTime.Parse(candidateStart).ToUniversalTime(),
            DateTime.Parse(candidateEnd).ToUniversalTime(),
            cancellationToken);

        Assert.Equal(expectOverlap, result.Any(v => v.Id == existingTrip.Id));
    }

    [Fact]
    public async Task GetOverlappingByVehicleIdAsync_DifferentVehicle_NeverMatches()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var (db, connection, organization, vehicle, driver, @event) = await SeedAsync(cancellationToken);
        using var _ = connection;
        using var __ = db;

        var otherVehicle = CreateVehicle(organization.Id, "XY999ZZ");
        db.Vehicles.Add(otherVehicle);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new TripRepository(db);
        var existingTrip = CreateTrip(
            organization.Id, driver.Id, vehicle.Id, @event.Id,
            new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 1, 14, 0, 0, DateTimeKind.Utc));
        await repository.AddAsync(existingTrip, CreateRoute(existingTrip.Id), [], cancellationToken);

        var result = await repository.GetOverlappingByVehicleIdAsync(
            otherVehicle.Id,
            new DateTime(2026, 11, 1, 11, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 1, 13, 0, 0, DateTimeKind.Utc),
            cancellationToken);

        Assert.Empty(result);
    }
}
