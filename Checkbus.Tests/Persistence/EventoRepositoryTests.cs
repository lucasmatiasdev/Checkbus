using Checkbus.ApiService.Domain.Entities.Rutas;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Infrastructure.Implementations.Repositories;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Tests.Persistence;

public class EventoRepositoryTests
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

    private static Evento CreateEvento(Guid ubicacionId) => new()
    {
        Id = Guid.NewGuid(),
        Nombre = "River vs Boca",
        Tipo = EventoTipo.Partido,
        Fecha = new DateTime(2026, 11, 1, 20, 0, 0, DateTimeKind.Utc),
        UbicacionId = ubicacionId
    };

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_RoundTrips()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var ubicacion = CreateUbicacion("ChIJ-evento");
        db.Ubicaciones.Add(ubicacion);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new EventoRepository(db);
        var evento = CreateEvento(ubicacion.Id);

        await repository.AddAsync(evento, cancellationToken);
        var result = await repository.GetByIdAsync(evento.Id, cancellationToken);

        Assert.NotNull(result);
        Assert.Equal(evento.Nombre, result!.Nombre);
        Assert.Equal(EventoTipo.Partido, result.Tipo);
        Assert.Equal(ubicacion.Id, result.UbicacionId);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new EventoRepository(db);

        var result = await repository.GetByIdAsync(Guid.NewGuid(), cancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_IsUnscoped_ReturnsEveryEvento()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var ubicacionA = CreateUbicacion("ChIJ-a");
        var ubicacionB = CreateUbicacion("ChIJ-b");
        db.Ubicaciones.AddRange(ubicacionA, ubicacionB);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new EventoRepository(db);
        await repository.AddAsync(CreateEvento(ubicacionA.Id), cancellationToken);
        await repository.AddAsync(CreateEvento(ubicacionB.Id), cancellationToken);

        var result = await repository.GetAllAsync(cancellationToken);

        Assert.Equal(2, result.Count);
    }
}
