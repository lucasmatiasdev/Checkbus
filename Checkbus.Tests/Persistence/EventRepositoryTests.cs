using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Infrastructure.Implementations.Repositories;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Tests.Persistence;

public class EventRepositoryTests
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

    private static Location CreateLocation(string placeId) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Estadio Monumental",
        Address = "Av. Pres. Figueroa Alcorta 7597",
        PlaceId = placeId,
        Latitude = -34.5453,
        Longitude = -58.4498
    };

    private static Organization CreateOrganization(string slug) => new()
    {
        Id = Guid.NewGuid(),
        CUIT = "20-12345678-9",
        Name = "Checkbus Demo",
        Slug = slug,
        LogoUrl = "",
        IsActive = true
    };

    private static Event CreateEvent(Guid organizationId, Guid locationId) => new()
    {
        Id = Guid.NewGuid(),
        Name = "River vs Boca",
        Type = EventType.Partido,
        Date = new DateTime(2026, 11, 1, 20, 0, 0, DateTimeKind.Utc),
        LocationId = locationId,
        OrganizationId = organizationId
    };

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_RoundTrips()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization("checkbus-demo");
        var location = CreateLocation("ChIJ-@event");
        db.Organizations.Add(organization);
        db.Locations.Add(location);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new EventRepository(db);
        var @event = CreateEvent(organization.Id, location.Id);

        await repository.AddAsync(@event, cancellationToken);
        var result = await repository.GetByIdAsync(@event.Id, cancellationToken);

        Assert.NotNull(result);
        Assert.Equal(@event.Name, result!.Name);
        Assert.Equal(EventType.Partido, result.Type);
        Assert.Equal(location.Id, result.LocationId);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new EventRepository(db);

        var result = await repository.GetByIdAsync(Guid.NewGuid(), cancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllByOrganizationAsync_ReturnsOnlyEventsFromThatOrganization()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organizationA = CreateOrganization("checkbus-demo");
        var organizationB = CreateOrganization("brahman");
        var locationA = CreateLocation("ChIJ-a");
        var locationB = CreateLocation("ChIJ-b");
        db.Organizations.AddRange(organizationA, organizationB);
        db.Locations.AddRange(locationA, locationB);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new EventRepository(db);
        await repository.AddAsync(CreateEvent(organizationA.Id, locationA.Id), cancellationToken);
        await repository.AddAsync(CreateEvent(organizationA.Id, locationB.Id), cancellationToken);
        await repository.AddAsync(CreateEvent(organizationB.Id, locationB.Id), cancellationToken);

        var result = await repository.GetAllByOrganizationAsync(organizationA.Id, cancellationToken);

        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Equal(organizationA.Id, e.OrganizationId));
    }
}
