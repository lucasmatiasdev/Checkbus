using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Rutas;
using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Infrastructure.Implementations.Repositories;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Tests.Persistence;

public class ViajeRepositoryTests
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

    private static User CreateChofer(Guid organizationId, string documentNumber) => new()
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

    private static Viaje CreateViaje(
        Guid organizationId, Guid choferId, Guid vehicleId, Guid eventoId,
        DateTime fechaSalida, DateTime fechaLlegada) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = organizationId,
        ChoferId = choferId,
        VehicleId = vehicleId,
        EventoId = eventoId,
        FechaSalida = fechaSalida,
        FechaLlegada = fechaLlegada,
        Capacidad = 45,
        AsientosDisponibles = 45,
        Precio = 1500m,
        Estado = ViajeEstado.Programado
    };

    private static Ruta CreateRuta(Guid viajeId) => new()
    {
        Id = Guid.NewGuid(),
        ViajeId = viajeId,
        TiempoEstimado = TimeSpan.FromHours(2),
        DistanciaKm = 120.5m
    };

    private static Stop CreateStop(Guid rutaId, Guid ubicacionId, int orden, StopTipo tipo) => new()
    {
        Id = Guid.NewGuid(),
        RutaId = rutaId,
        UbicacionId = ubicacionId,
        Orden = orden,
        Tipo = tipo
    };

    private async Task<(CheckbusDbContext Db, SqliteConnection Connection, Organization Organization, Vehicle Vehicle, User Chofer, Evento Evento)> SeedAsync(CancellationToken cancellationToken)
    {
        var connection = CreateOpenConnection();
        var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization("checkbus-demo");
        var vehicle = CreateVehicle(organization.Id, "AB123CD");
        var chofer = CreateChofer(organization.Id, "30111222");
        var ubicacion = CreateUbicacion("ChIJ-viaje");
        db.Organizations.Add(organization);
        db.Vehicles.Add(vehicle);
        db.Users.Add(chofer);
        db.Ubicaciones.Add(ubicacion);
        await db.SaveChangesAsync(cancellationToken);

        var evento = CreateEvento(ubicacion.Id);
        db.Eventos.Add(evento);
        await db.SaveChangesAsync(cancellationToken);

        return (db, connection, organization, vehicle, chofer, evento);
    }

    [Fact]
    public async Task AddAsync_PersistsViajeRutaAndStops()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var (db, connection, organization, vehicle, chofer, evento) = await SeedAsync(cancellationToken);
        using var _ = connection;
        using var __ = db;

        var repository = new ViajeRepository(db);
        var viaje = CreateViaje(
            organization.Id, chofer.Id, vehicle.Id, evento.Id,
            new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 1, 14, 0, 0, DateTimeKind.Utc));
        var ruta = CreateRuta(viaje.Id);
        var stops = new[]
        {
            CreateStop(ruta.Id, evento.UbicacionId, 0, StopTipo.Origen),
            CreateStop(ruta.Id, evento.UbicacionId, 1, StopTipo.Destino)
        };

        await repository.AddAsync(viaje, ruta, stops, cancellationToken);

        var persistedViaje = await repository.GetByIdAsync(viaje.Id, cancellationToken);
        var persistedRuta = await db.Rutas.FirstOrDefaultAsync(r => r.ViajeId == viaje.Id, cancellationToken);
        var persistedStops = await db.Stops.Where(s => s.RutaId == ruta.Id).ToListAsync(cancellationToken);

        Assert.NotNull(persistedViaje);
        Assert.NotNull(persistedRuta);
        Assert.Equal(2, persistedStops.Count);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new ViajeRepository(db);

        var result = await repository.GetByIdAsync(Guid.NewGuid(), cancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllByOrganizationAsync_ReturnsOnlyThatOrganizationsViajes()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var (db, connection, organization, vehicle, chofer, evento) = await SeedAsync(cancellationToken);
        using var _ = connection;
        using var __ = db;

        var otherOrganization = CreateOrganization("other-org");
        db.Organizations.Add(otherOrganization);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new ViajeRepository(db);
        var viajeInOrg = CreateViaje(
            organization.Id, chofer.Id, vehicle.Id, evento.Id,
            new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 1, 14, 0, 0, DateTimeKind.Utc));
        var viajeInOtherOrg = CreateViaje(
            otherOrganization.Id, chofer.Id, vehicle.Id, evento.Id,
            new DateTime(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 2, 14, 0, 0, DateTimeKind.Utc));
        await repository.AddAsync(viajeInOrg, CreateRuta(viajeInOrg.Id), [], cancellationToken);
        await repository.AddAsync(viajeInOtherOrg, CreateRuta(viajeInOtherOrg.Id), [], cancellationToken);

        var result = await repository.GetAllByOrganizationAsync(organization.Id, cancellationToken);

        Assert.Single(result);
        Assert.Equal(viajeInOrg.Id, result[0].Id);
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
        var (db, connection, organization, vehicle, chofer, evento) = await SeedAsync(cancellationToken);
        using var _ = connection;
        using var __ = db;

        var repository = new ViajeRepository(db);
        var existingViaje = CreateViaje(
            organization.Id, chofer.Id, vehicle.Id, evento.Id,
            new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 1, 14, 0, 0, DateTimeKind.Utc));
        await repository.AddAsync(existingViaje, CreateRuta(existingViaje.Id), [], cancellationToken);

        var result = await repository.GetOverlappingByVehicleIdAsync(
            vehicle.Id,
            DateTime.Parse(candidateStart).ToUniversalTime(),
            DateTime.Parse(candidateEnd).ToUniversalTime(),
            cancellationToken);

        Assert.Equal(expectOverlap, result.Any(v => v.Id == existingViaje.Id));
    }

    [Theory]
    [InlineData("2026-11-01T11:00:00Z", "2026-11-01T13:00:00Z", true)]
    [InlineData("2026-11-01T14:00:00Z", "2026-11-01T16:00:00Z", true)]
    [InlineData("2026-11-01T08:00:00Z", "2026-11-01T10:00:00Z", true)]
    [InlineData("2026-11-01T06:00:00Z", "2026-11-01T09:00:00Z", false)]
    [InlineData("2026-11-01T15:00:00Z", "2026-11-01T17:00:00Z", false)]
    public async Task GetOverlappingByChoferIdAsync_DetectsOverlapIncludingBoundaries(
        string candidateStart, string candidateEnd, bool expectOverlap)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var (db, connection, organization, vehicle, chofer, evento) = await SeedAsync(cancellationToken);
        using var _ = connection;
        using var __ = db;

        var repository = new ViajeRepository(db);
        var existingViaje = CreateViaje(
            organization.Id, chofer.Id, vehicle.Id, evento.Id,
            new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 1, 14, 0, 0, DateTimeKind.Utc));
        await repository.AddAsync(existingViaje, CreateRuta(existingViaje.Id), [], cancellationToken);

        var result = await repository.GetOverlappingByChoferIdAsync(
            chofer.Id,
            DateTime.Parse(candidateStart).ToUniversalTime(),
            DateTime.Parse(candidateEnd).ToUniversalTime(),
            cancellationToken);

        Assert.Equal(expectOverlap, result.Any(v => v.Id == existingViaje.Id));
    }

    [Fact]
    public async Task GetOverlappingByVehicleIdAsync_DifferentVehicle_NeverMatches()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var (db, connection, organization, vehicle, chofer, evento) = await SeedAsync(cancellationToken);
        using var _ = connection;
        using var __ = db;

        var otherVehicle = CreateVehicle(organization.Id, "XY999ZZ");
        db.Vehicles.Add(otherVehicle);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new ViajeRepository(db);
        var existingViaje = CreateViaje(
            organization.Id, chofer.Id, vehicle.Id, evento.Id,
            new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 1, 14, 0, 0, DateTimeKind.Utc));
        await repository.AddAsync(existingViaje, CreateRuta(existingViaje.Id), [], cancellationToken);

        var result = await repository.GetOverlappingByVehicleIdAsync(
            otherVehicle.Id,
            new DateTime(2026, 11, 1, 11, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 1, 13, 0, 0, DateTimeKind.Utc),
            cancellationToken);

        Assert.Empty(result);
    }
}
