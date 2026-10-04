using Checkbus.ApiService.Domain.Entities.Rutas;
using Checkbus.ApiService.Infrastructure.Implementations.Repositories;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Tests.Persistence;

public class UbicacionRepositoryTests
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

    private static Ubicacion CreateUbicacion(string placeId) => new()
    {
        Id = Guid.NewGuid(),
        Nombre = "Estadio Monumental",
        Direccion = "Av. Pres. Figueroa Alcorta 7597",
        PlaceId = placeId,
        Latitud = -34.5453,
        Longitud = -58.4498
    };

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_RoundTrips()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new UbicacionRepository(db);
        var ubicacion = CreateUbicacion("ChIJ123");

        await repository.AddAsync(ubicacion, cancellationToken);
        var result = await repository.GetByIdAsync(ubicacion.Id, cancellationToken);

        Assert.NotNull(result);
        Assert.Equal(ubicacion.Nombre, result!.Nombre);
        Assert.Equal(ubicacion.PlaceId, result.PlaceId);
        Assert.Equal(ubicacion.Latitud, result.Latitud);
        Assert.Equal(ubicacion.Longitud, result.Longitud);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new UbicacionRepository(db);

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

        var repository = new UbicacionRepository(db);

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

        var repository = new UbicacionRepository(db);
        var ubicacion = CreateUbicacion("ChIJ456");
        await repository.AddAsync(ubicacion, cancellationToken);

        var result = await repository.FindByPlaceIdAsync("ChIJ456", cancellationToken);

        Assert.NotNull(result);
        Assert.Equal(ubicacion.Id, result!.Id);
    }

    [Fact]
    public async Task AddAsync_DuplicatePlaceId_ThrowsBecauseOfUniqueIndex()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new UbicacionRepository(db);
        await repository.AddAsync(CreateUbicacion("ChIJ-dup"), cancellationToken);

        await Assert.ThrowsAnyAsync<DbUpdateException>(
            () => repository.AddAsync(CreateUbicacion("ChIJ-dup"), cancellationToken));
    }
}
