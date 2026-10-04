using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Viajes.Queries;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Rutas;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.Tests.Viajes;

public class GetViajesQueryHandlerTests
{
    private sealed class FakeViajeRepository(IReadOnlyList<Viaje> viajes) : IViajeRepository
    {
        public Task AddAsync(Viaje viaje, Ruta ruta, IEnumerable<Stop> stops, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajesQueryHandler.");

        public Task<Viaje?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajesQueryHandler.");

        public Task<IReadOnlyList<Viaje>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => Task.FromResult(viajes.Where(v => v.OrganizationId == organizationId).ToList() as IReadOnlyList<Viaje>);

        public Task<IReadOnlyList<Viaje>> GetOverlappingByVehicleIdAsync(Guid vehicleId, DateTime fechaSalida, DateTime fechaLlegada, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajesQueryHandler.");

        public Task<IReadOnlyList<Viaje>> GetOverlappingByChoferIdAsync(Guid choferId, DateTime fechaSalida, DateTime fechaLlegada, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajesQueryHandler.");

        public Task<Ruta?> GetRutaByViajeIdAsync(Guid viajeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajesQueryHandler.");

        public Task<IReadOnlyList<Stop>> GetStopsByRutaIdAsync(Guid rutaId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajesQueryHandler.");
    }

    private sealed class FakeVehicleRepository(IReadOnlyList<Vehicle> vehicles) : IVehicleRepository
    {
        public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajesQueryHandler.");

        public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajesQueryHandler.");

        public Task<IReadOnlyList<Vehicle>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => Task.FromResult(vehicles);

        public Task<bool> PatentExistsAsync(string patent, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajesQueryHandler.");
    }

    private sealed class FakeUserRepository(IReadOnlyList<User> users) : IUserRepository
    {
        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajesQueryHandler.");

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajesQueryHandler.");

        public Task AddAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajesQueryHandler.");

        public Task UpdateAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajesQueryHandler.");

        public Task<bool> DocumentNumberExistsInOrganizationAsync(string documentNumber, Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajesQueryHandler.");

        public Task<IReadOnlyList<string>> FindEmailsByLocalPartPrefixAsync(string localPartPrefix, string domain, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajesQueryHandler.");

        public Task<IReadOnlyList<User>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => Task.FromResult(users);
    }

    private sealed class FakeEventoRepository(IReadOnlyList<Evento> eventos) : IEventoRepository
    {
        public Task AddAsync(Evento evento, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajesQueryHandler.");

        public Task<Evento?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(eventos.FirstOrDefault(e => e.Id == id));

        public Task<IReadOnlyList<Evento>> GetAllAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetViajesQueryHandler.");
    }

    private sealed class FakeCurrentUserService(Guid? organizationId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.NewGuid();
        public Guid? OrganizationId => organizationId;
        public string? Role => "Planificador";
        public string? Email => "planificador@checkbus-demo.com";
    }

    private static Vehicle CreateVehicle(Guid id, Guid organizationId) => new()
    {
        Id = id,
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

    private static User CreateChofer(Guid id, Guid organizationId) => new()
    {
        Id = id,
        Name = "Juan",
        Surname = "Perez",
        Email = "juan.perez@checkbus-demo.com",
        PasswordHash = "hashed-password",
        DocumentNumber = "30123456",
        Role = Role.Chofer,
        OrganizationId = organizationId,
        IsActive = true
    };

    private static Evento CreateEvento(Guid id) => new()
    {
        Id = id,
        Nombre = "Final Copa Argentina",
        Tipo = EventoTipo.Partido,
        Fecha = DateTime.UtcNow,
        UbicacionId = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static Viaje CreateViaje(Guid organizationId, Guid vehicleId, Guid choferId, Guid eventoId) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = organizationId,
        ChoferId = choferId,
        VehicleId = vehicleId,
        EventoId = eventoId,
        FechaSalida = DateTime.UtcNow,
        FechaLlegada = DateTime.UtcNow.AddHours(8),
        Capacidad = 45,
        AsientosDisponibles = 45,
        Precio = 15000m
    };

    [Fact]
    public async Task Handle_ReturnsViajesForCallersOrganization_WithResolvedJoins()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(Guid.NewGuid(), organizationId);
        var chofer = CreateChofer(Guid.NewGuid(), organizationId);
        var evento = CreateEvento(Guid.NewGuid());
        var viaje = CreateViaje(organizationId, vehicle.Id, chofer.Id, evento.Id);

        var handler = new GetViajesQueryHandler(
            new FakeViajeRepository([viaje]),
            new FakeVehicleRepository([vehicle]),
            new FakeUserRepository([chofer]),
            new FakeEventoRepository([evento]),
            new FakeCurrentUserService(organizationId));

        var result = await handler.Handle(new GetViajesQuery(), TestContext.Current.CancellationToken);

        var dto = Assert.Single(result);
        Assert.Equal(viaje.Id, dto.Id);
        Assert.Equal(vehicle.Patent, dto.VehiclePatent);
        Assert.Equal(chofer.Name, dto.ChoferName);
        Assert.Equal(evento.Nombre, dto.EventoNombre);
    }

    [Fact]
    public async Task Handle_NoViajes_ReturnsEmptyList()
    {
        var organizationId = Guid.NewGuid();
        var handler = new GetViajesQueryHandler(
            new FakeViajeRepository([]),
            new FakeVehicleRepository([]),
            new FakeUserRepository([]),
            new FakeEventoRepository([]),
            new FakeCurrentUserService(organizationId));

        var result = await handler.Handle(new GetViajesQuery(), TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var handler = new GetViajesQueryHandler(
            new FakeViajeRepository([]),
            new FakeVehicleRepository([]),
            new FakeUserRepository([]),
            new FakeEventoRepository([]),
            new FakeCurrentUserService(null));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new GetViajesQuery(), TestContext.Current.CancellationToken));
    }
}
