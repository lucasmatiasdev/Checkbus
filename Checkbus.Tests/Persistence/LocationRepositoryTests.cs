using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Infrastructure.Implementations.Repositories;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Tests.Persistence;

public class LocationRepositoryTests
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

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_RoundTrips()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new LocationRepository(db);
        var location = CreateLocation("ChIJ123");

        await repository.AddAsync(location, cancellationToken);
        var result = await repository.GetByIdAsync(location.Id, cancellationToken);

        Assert.NotNull(result);
        Assert.Equal(location.Name, result!.Name);
        Assert.Equal(location.PlaceId, result.PlaceId);
        Assert.Equal(location.Latitude, result.Latitude);
        Assert.Equal(location.Longitude, result.Longitude);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new LocationRepository(db);

        var result = await repository.GetByIdAsync(Guid.NewGuid(), cancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindByPlaceIdAsync_Absent_ReturnsNull()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new LocationRepository(db);

        var result = await repository.FindByPlaceIdAsync("ChIJ-does-not-exist", cancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindByPlaceIdAsync_Present_ReturnsRow()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new LocationRepository(db);
        var location = CreateLocation("ChIJ456");
        await repository.AddAsync(location, cancellationToken);

        var result = await repository.FindByPlaceIdAsync("ChIJ456", cancellationToken);

        Assert.NotNull(result);
        Assert.Equal(location.Id, result!.Id);
    }

    [Fact]
    public async Task AddAsync_DuplicatePlaceId_ThrowsBecauseOfUniqueIndex()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new LocationRepository(db);
        await repository.AddAsync(CreateLocation("ChIJ-dup"), cancellationToken);

        await Assert.ThrowsAnyAsync<DbUpdateException>(
            () => repository.AddAsync(CreateLocation("ChIJ-dup"), cancellationToken));
    }
}
