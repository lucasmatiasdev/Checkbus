using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Viajes.Queries;
using Checkbus.ApiService.Domain.Entities.Rutas;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Exceptions.Rutas;

namespace Checkbus.Tests.Viajes;

public class GetViajeQueryHandlerTests
{
    private sealed class FakeViajeRepository(Viaje? viaje, Ruta? ruta, IReadOnlyList<Stop> stops) : IViajeRepository
    {
        public Task AddAsync(Viaje viaje, Ruta ruta, IEnumerable<Stop> stops, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajeQueryHandler.");

        public Task<Viaje?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(viaje);

        public Task<IReadOnlyList<Viaje>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajeQueryHandler.");

        public Task<IReadOnlyList<Viaje>> GetOverlappingByVehicleIdAsync(Guid vehicleId, DateTime fechaSalida, DateTime fechaLlegada, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajeQueryHandler.");

        public Task<IReadOnlyList<Viaje>> GetOverlappingByChoferIdAsync(Guid choferId, DateTime fechaSalida, DateTime fechaLlegada, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajeQueryHandler.");

        public Task<Ruta?> GetRutaByViajeIdAsync(Guid viajeId, CancellationToken cancellationToken) => Task.FromResult(ruta);

        public Task<IReadOnlyList<Stop>> GetStopsByRutaIdAsync(Guid rutaId, CancellationToken cancellationToken) => Task.FromResult(stops);
    }

    private sealed class FakeVehicleRepository(Vehicle? vehicle) : IVehicleRepository
    {
        public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(vehicle);

        public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajeQueryHandler.");

        public Task<IReadOnlyList<Vehicle>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajeQueryHandler.");

        public Task<bool> PatentExistsAsync(string patent, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajeQueryHandler.");
    }

    private sealed class FakeUserRepository(User? chofer) : IUserRepository
    {
        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajeQueryHandler.");

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(chofer);

        public Task AddAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajeQueryHandler.");

        public Task UpdateAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajeQueryHandler.");

        public Task<bool> DocumentNumberExistsInOrganizationAsync(string documentNumber, Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajeQueryHandler.");

        public Task<IReadOnlyList<string>> FindEmailsByLocalPartPrefixAsync(string localPartPrefix, string domain, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajeQueryHandler.");

        public Task<IReadOnlyList<User>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajeQueryHandler.");
    }

    private sealed class FakeEventoRepository(Evento? evento) : IEventoRepository
    {
        public Task AddAsync(Evento evento, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajeQueryHandler.");

        public Task<Evento?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(evento);

        public Task<IReadOnlyList<Evento>> GetAllAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajeQueryHandler.");
    }

    private sealed class FakeUbicacionRepository(Dictionary<Guid, Ubicacion> ubicacionesById) : IUbicacionRepository
    {
        public Task<Ubicacion?> FindByPlaceIdAsync(string placeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajeQueryHandler.");

        public Task AddAsync(Ubicacion ubicacion, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajeQueryHandler.");

        public Task<Ubicacion?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(ubicacionesById.GetValueOrDefault(id));
    }

    private sealed class FakeCurrentUserService(Guid? organizationId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.NewGuid();
        public Guid? OrganizationId => organizationId;
        public string? Role => "Planificador";
        public string? Email => "planificador@checkbus-demo.com";
    }

    private static GetViajeQueryHandler BuildHandler(
        Guid organizationId,
        Viaje? viaje,
        Ruta? ruta = null,
        IReadOnlyList<Stop>? stops = null,
        Vehicle? vehicle = null,
        User? chofer = null,
        Evento? evento = null,
        Dictionary<Guid, Ubicacion>? ubicacionesById = null) => new(
            new FakeViajeRepository(viaje, ruta, stops ?? []),
            new FakeVehicleRepository(vehicle),
            new FakeUserRepository(chofer),
            new FakeEventoRepository(evento),
            new FakeUbicacionRepository(ubicacionesById ?? []),
            new FakeCurrentUserService(organizationId));

    [Fact]
    public async Task Handle_ViajeFound_ReturnsDetailWithRutaAndStops()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = new Vehicle { Id = Guid.NewGuid(), Brand = "Mercedes-Benz", Model = "O500", Patent = "AB123CD", OrganizationId = organizationId, OwnerType = VehicleOwnerType.Organizacion };
        var chofer = new User { Id = Guid.NewGuid(), Name = "Juan", Surname = "Perez", Email = "j@p.com", PasswordHash = "x", DocumentNumber = "1", Role = Role.Chofer, OrganizationId = organizationId };
        var evento = new Evento { Id = Guid.NewGuid(), Nombre = "Final Copa Argentina", Tipo = EventoTipo.Partido, Fecha = DateTime.UtcNow, UbicacionId = Guid.NewGuid() };
        var viaje = new Viaje
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ChoferId = chofer.Id,
            VehicleId = vehicle.Id,
            EventoId = evento.Id,
            FechaSalida = DateTime.UtcNow,
            FechaLlegada = DateTime.UtcNow.AddHours(8),
            Capacidad = 45,
            AsientosDisponibles = 45,
            Precio = 15000m
        };
        var ruta = new Ruta { Id = Guid.NewGuid(), ViajeId = viaje.Id, TiempoEstimado = TimeSpan.FromHours(8), DistanciaKm = 700m };
        var ubicacionOrigen = new Ubicacion { Id = Guid.NewGuid(), Nombre = "Terminal Retiro", Direccion = "Dir 1", PlaceId = "place-1", Latitud = -34.5, Longitud = -58.3 };
        var ubicacionDestino = new Ubicacion { Id = Guid.NewGuid(), Nombre = "Estadio Kempes", Direccion = "Dir 2", PlaceId = "place-2", Latitud = -31.3, Longitud = -64.2 };
        var stops = new List<Stop>
        {
            new() { Id = Guid.NewGuid(), RutaId = ruta.Id, UbicacionId = ubicacionOrigen.Id, Orden = 0, Tipo = StopTipo.Origen },
            new() { Id = Guid.NewGuid(), RutaId = ruta.Id, UbicacionId = ubicacionDestino.Id, Orden = 1, Tipo = StopTipo.Destino }
        };

        var handler = BuildHandler(
            organizationId, viaje, ruta, stops, vehicle, chofer, evento,
            new Dictionary<Guid, Ubicacion> { [ubicacionOrigen.Id] = ubicacionOrigen, [ubicacionDestino.Id] = ubicacionDestino });

        var result = await handler.Handle(new GetViajeQuery { Id = viaje.Id }, TestContext.Current.CancellationToken);

        Assert.Equal(viaje.Id, result.Id);
        Assert.Equal(ruta.TiempoEstimado, result.Ruta.TiempoEstimado);
        Assert.Equal(ruta.DistanciaKm, result.Ruta.DistanciaKm);
        Assert.Equal(2, result.Ruta.Stops.Count);
        Assert.Equal("Terminal Retiro", result.Ruta.Stops[0].Nombre);
        Assert.Equal(StopTipo.Origen, result.Ruta.Stops[0].Tipo);
        Assert.Equal("Estadio Kempes", result.Ruta.Stops[1].Nombre);
        Assert.Equal(StopTipo.Destino, result.Ruta.Stops[1].Tipo);
    }

    [Fact]
    public async Task Handle_ViajeNotFound_ThrowsViajeNotFound()
    {
        var handler = BuildHandler(Guid.NewGuid(), viaje: null);

        await Assert.ThrowsAsync<ViajeNotFoundException>(() =>
            handler.Handle(new GetViajeQuery { Id = Guid.NewGuid() }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ViajeBelongsToDifferentOrganization_ThrowsViajeNotFound()
    {
        var viaje = new Viaje
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            ChoferId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            EventoId = Guid.NewGuid(),
            FechaSalida = DateTime.UtcNow,
            FechaLlegada = DateTime.UtcNow.AddHours(8)
        };

        var handler = BuildHandler(Guid.NewGuid(), viaje);

        await Assert.ThrowsAsync<ViajeNotFoundException>(() =>
            handler.Handle(new GetViajeQuery { Id = viaje.Id }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var handler = new GetViajeQueryHandler(
            new FakeViajeRepository(null, null, []),
            new FakeVehicleRepository(null),
            new FakeUserRepository(null),
            new FakeEventoRepository(null),
            new FakeUbicacionRepository([]),
            new FakeCurrentUserService(null));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new GetViajeQuery { Id = Guid.NewGuid() }, TestContext.Current.CancellationToken));
    }
}
