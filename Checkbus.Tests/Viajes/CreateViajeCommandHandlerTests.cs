using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Maps;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Viajes.Commands;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Documents;
using Checkbus.ApiService.Domain.Entities.Rutas;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Rutas;
using Checkbus.ApiService.Domain.Exceptions.VehicleDocuments;

namespace Checkbus.Tests.Viajes;

public class CreateViajeCommandHandlerTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();

    private sealed class FakeVehicleRepository(Vehicle? vehicle) : IVehicleRepository
    {
        public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(vehicle);

        public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateViajeCommandHandler.");

        public Task<IReadOnlyList<Vehicle>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateViajeCommandHandler.");

        public Task<bool> PatentExistsAsync(string patent, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateViajeCommandHandler.");
    }

    private sealed class FakeVehicleDocumentRepository(IReadOnlyList<VehicleDocument> documents) : IVehicleDocumentRepository
    {
        public Task<IReadOnlyList<VehicleDocument>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken)
            => Task.FromResult(documents);

        public Task AddRangeAsync(IEnumerable<VehicleDocument> documents, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateViajeCommandHandler.");

        public Task AddAsync(VehicleDocument document, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateViajeCommandHandler.");

        public Task UpdateAsync(VehicleDocument document, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateViajeCommandHandler.");

        public Task<int> GetExpiringOrExpiredCountByOrganizationAsync(
            Guid organizationId, DateOnly expiringThresholdDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateViajeCommandHandler.");
    }

    private sealed class FakeUserRepository(User? chofer) : IUserRepository
    {
        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateViajeCommandHandler.");

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(chofer);

        public Task AddAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateViajeCommandHandler.");

        public Task UpdateAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateViajeCommandHandler.");

        public Task<bool> DocumentNumberExistsInOrganizationAsync(string documentNumber, Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateViajeCommandHandler.");

        public Task<IReadOnlyList<string>> FindEmailsByLocalPartPrefixAsync(string localPartPrefix, string domain, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateViajeCommandHandler.");

        public Task<IReadOnlyList<User>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateViajeCommandHandler.");
    }

    private sealed class FakeDriverRequirementRepository(IReadOnlyList<DriverRequirement> requirements) : IDriverRequirementRepository
    {
        public Task<IReadOnlyList<DriverRequirement>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(requirements);

        public Task AddRangeAsync(IEnumerable<DriverRequirement> requirements, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateViajeCommandHandler.");

        public Task UpdateAsync(DriverRequirement requirement, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateViajeCommandHandler.");

        public Task<int> GetExpiringOrExpiredCountByOrganizationAsync(
            Guid organizationId, DateOnly expiringThresholdDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateViajeCommandHandler.");
    }

    private sealed class FakeEventoRepository(Evento? evento) : IEventoRepository
    {
        public Task AddAsync(Evento evento, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateViajeCommandHandler.");

        public Task<Evento?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(evento);

        public Task<IReadOnlyList<Evento>> GetAllAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateViajeCommandHandler.");
    }

    private sealed class FakeUbicacionRepository : IUbicacionRepository
    {
        public List<Ubicacion> AddedUbicaciones { get; } = [];

        public Task<Ubicacion?> FindByPlaceIdAsync(string placeId, CancellationToken cancellationToken)
            => Task.FromResult<Ubicacion?>(null);

        public Task AddAsync(Ubicacion ubicacion, CancellationToken cancellationToken)
        {
            AddedUbicaciones.Add(ubicacion);
            return Task.CompletedTask;
        }

        public Task<Ubicacion?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateViajeCommandHandler.");
    }

    private sealed class FakeViajeRepository : IViajeRepository
    {
        public IReadOnlyList<Viaje> VehicleOverlaps { get; set; } = [];
        public IReadOnlyList<Viaje> ChoferOverlaps { get; set; } = [];

        public Viaje? AddedViaje { get; private set; }
        public Ruta? AddedRuta { get; private set; }
        public IReadOnlyList<Stop> AddedStops { get; private set; } = [];

        public Task AddAsync(Viaje viaje, Ruta ruta, IEnumerable<Stop> stops, CancellationToken cancellationToken)
        {
            AddedViaje = viaje;
            AddedRuta = ruta;
            AddedStops = stops.ToList();
            return Task.CompletedTask;
        }

        public Task<Viaje?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateViajeCommandHandler.");

        public Task<IReadOnlyList<Viaje>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateViajeCommandHandler.");

        public Task<IReadOnlyList<Viaje>> GetOverlappingByVehicleIdAsync(
            Guid vehicleId, DateTime fechaSalida, DateTime fechaLlegada, CancellationToken cancellationToken)
            => Task.FromResult(VehicleOverlaps);

        public Task<IReadOnlyList<Viaje>> GetOverlappingByChoferIdAsync(
            Guid choferId, DateTime fechaSalida, DateTime fechaLlegada, CancellationToken cancellationToken)
            => Task.FromResult(ChoferOverlaps);

        public Task<Ruta?> GetRutaByViajeIdAsync(Guid viajeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateViajeCommandHandler.");

        public Task<IReadOnlyList<Stop>> GetStopsByRutaIdAsync(Guid rutaId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateViajeCommandHandler.");
    }

    private sealed class FakeDirectionsService(DirectionsResult result) : IDirectionsService
    {
        public IReadOnlyList<(double Latitude, double Longitude)>? CalledWithPoints { get; private set; }

        public Task<DirectionsResult> GetDirectionsAsync(
            IReadOnlyList<(double Latitude, double Longitude)> points, CancellationToken cancellationToken)
        {
            CalledWithPoints = points;
            return Task.FromResult(result);
        }
    }

    private sealed class FakeCurrentUserService(Guid? organizationId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.NewGuid();
        public Guid? OrganizationId => organizationId;
        public string? Role => "Planificador";
        public string? Email => "planificador@checkbus-demo.com";
    }

    private static Vehicle CreateVehicle(VehicleStatus status = VehicleStatus.Activo, int capacity = 45) => new()
    {
        Id = Guid.NewGuid(),
        Brand = "Mercedes-Benz",
        Model = "O500",
        Year = 2020,
        Patent = "AB123CD",
        Capacity = capacity,
        Mileage = 1000,
        Status = status,
        OrganizationId = OrganizationId,
        OwnerType = VehicleOwnerType.Organizacion
    };

    private static VehicleDocument CreateVehicleDocument(
        Guid vehicleId, VehicleDocumentStatus status = VehicleDocumentStatus.Apto, DateOnly? expirationDate = null) => new()
    {
        Id = Guid.NewGuid(),
        VehicleId = vehicleId,
        Type = VehicleDocumentType.Seguro,
        Status = status,
        ExpirationDate = expirationDate,
        DocumentPresent = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static User CreateChofer(bool isActive = true, Role role = Role.Chofer) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Juan",
        Surname = "Perez",
        Email = "juan.perez@checkbus-demo.com",
        PasswordHash = "hashed-password",
        DocumentNumber = "30123456",
        Role = role,
        OrganizationId = OrganizationId,
        IsActive = isActive
    };

    private static DriverRequirement CreateDriverRequirement(
        Guid userId, DriverRequirementStatus status = DriverRequirementStatus.Apto, DateOnly? expirationDate = null) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Type = DriverRequirementType.LicenciaConducir,
        Status = status,
        ExpirationDate = expirationDate,
        DocumentPresent = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static Evento CreateEvento() => new()
    {
        Id = Guid.NewGuid(),
        Nombre = "Final Copa Argentina",
        Tipo = EventoTipo.Partido,
        Fecha = new DateTime(2026, 5, 1, 20, 0, 0, DateTimeKind.Utc),
        UbicacionId = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static CreateViajeCommand CreateCommand(Guid vehicleId, Guid choferId, Guid eventoId) => new()
    {
        VehicleId = vehicleId,
        ChoferId = choferId,
        EventoId = eventoId,
        FechaSalida = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc),
        FechaLlegada = new DateTime(2026, 5, 1, 18, 0, 0, DateTimeKind.Utc),
        Precio = 15000m,
        Stops =
        [
            new CreateViajeStopCommand { Nombre = "Terminal Retiro", Direccion = "Av. Dr. Jose Maria Ramos Mejia 1680", PlaceId = "place-retiro", Latitud = -34.5921, Longitud = -58.3747 },
            new CreateViajeStopCommand { Nombre = "Estadio Mario Alberto Kempes", Direccion = "Av. Cardeñosa, Córdoba", PlaceId = "place-kempes", Latitud = -31.3333, Longitud = -64.2333 }
        ]
    };

    private sealed class Harness
    {
        public FakeVehicleRepository VehicleRepository { get; set; } = new(null);
        public FakeVehicleDocumentRepository VehicleDocumentRepository { get; set; } = new([]);
        public FakeUserRepository UserRepository { get; set; } = new(null);
        public FakeDriverRequirementRepository DriverRequirementRepository { get; set; } = new([]);
        public FakeEventoRepository EventoRepository { get; set; } = new(null);
        public FakeUbicacionRepository UbicacionRepository { get; set; } = new();
        public FakeViajeRepository ViajeRepository { get; set; } = new();
        public FakeDirectionsService DirectionsService { get; set; } = new(new DirectionsResult(TimeSpan.FromHours(8), 700m));
        public Guid? OrganizationId { get; set; } = CreateViajeCommandHandlerTests.OrganizationId;

        public CreateViajeCommandHandler BuildHandler() => new(
            VehicleRepository,
            VehicleDocumentRepository,
            UserRepository,
            DriverRequirementRepository,
            EventoRepository,
            UbicacionRepository,
            ViajeRepository,
            DirectionsService,
            new FakeCurrentUserService(OrganizationId));
    }

    // ---- Vehicle habilitación ----

    [Fact]
    public async Task Handle_InactiveVehicle_ThrowsViajeNotHabilitado()
    {
        var vehicle = CreateVehicle(VehicleStatus.Inactivo);
        var chofer = CreateChofer();
        var harness = new Harness
        {
            VehicleRepository = new FakeVehicleRepository(vehicle),
            UserRepository = new FakeUserRepository(chofer),
            EventoRepository = new FakeEventoRepository(CreateEvento())
        };

        await Assert.ThrowsAsync<ViajeNotHabilitadoException>(() =>
            harness.BuildHandler().Handle(CreateCommand(vehicle.Id, chofer.Id, Guid.NewGuid()), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_VehicleEnMantenimiento_IsNotRejected()
    {
        var vehicle = CreateVehicle(VehicleStatus.EnMantenimiento);
        var chofer = CreateChofer();
        var harness = new Harness
        {
            VehicleRepository = new FakeVehicleRepository(vehicle),
            UserRepository = new FakeUserRepository(chofer),
            EventoRepository = new FakeEventoRepository(CreateEvento())
        };

        var result = await harness.BuildHandler().Handle(
            CreateCommand(vehicle.Id, chofer.Id, Guid.NewGuid()), TestContext.Current.CancellationToken);

        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task Handle_ExpiredVehicleDocument_ThrowsViajeNotHabilitado()
    {
        var vehicle = CreateVehicle();
        var chofer = CreateChofer();
        var command = CreateCommand(vehicle.Id, chofer.Id, Guid.NewGuid());
        var expiredDocument = CreateVehicleDocument(
            vehicle.Id, VehicleDocumentStatus.Apto, DateOnly.FromDateTime(command.FechaLlegada).AddDays(-1));
        var harness = new Harness
        {
            VehicleRepository = new FakeVehicleRepository(vehicle),
            VehicleDocumentRepository = new FakeVehicleDocumentRepository([expiredDocument]),
            UserRepository = new FakeUserRepository(chofer),
            EventoRepository = new FakeEventoRepository(CreateEvento())
        };

        await Assert.ThrowsAsync<ViajeNotHabilitadoException>(() =>
            harness.BuildHandler().Handle(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_VehicleDateOverlap_ThrowsViajeNotHabilitado()
    {
        var vehicle = CreateVehicle();
        var chofer = CreateChofer();
        var harness = new Harness
        {
            VehicleRepository = new FakeVehicleRepository(vehicle),
            UserRepository = new FakeUserRepository(chofer),
            EventoRepository = new FakeEventoRepository(CreateEvento()),
            ViajeRepository = new FakeViajeRepository { VehicleOverlaps = [new Viaje { Id = Guid.NewGuid(), OrganizationId = OrganizationId, ChoferId = Guid.NewGuid(), VehicleId = vehicle.Id, EventoId = Guid.NewGuid(), FechaSalida = DateTime.UtcNow, FechaLlegada = DateTime.UtcNow }] }
        };

        await Assert.ThrowsAsync<ViajeNotHabilitadoException>(() =>
            harness.BuildHandler().Handle(CreateCommand(vehicle.Id, chofer.Id, Guid.NewGuid()), TestContext.Current.CancellationToken));
    }

    // ---- Chofer habilitación ----

    [Fact]
    public async Task Handle_InactiveChoferAccount_ThrowsViajeNotHabilitado()
    {
        var vehicle = CreateVehicle();
        var chofer = CreateChofer(isActive: false);
        var harness = new Harness
        {
            VehicleRepository = new FakeVehicleRepository(vehicle),
            UserRepository = new FakeUserRepository(chofer),
            EventoRepository = new FakeEventoRepository(CreateEvento())
        };

        await Assert.ThrowsAsync<ViajeNotHabilitadoException>(() =>
            harness.BuildHandler().Handle(CreateCommand(vehicle.Id, chofer.Id, Guid.NewGuid()), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ExpiredDriverRequirement_ThrowsViajeNotHabilitado()
    {
        var vehicle = CreateVehicle();
        var chofer = CreateChofer();
        var command = CreateCommand(vehicle.Id, chofer.Id, Guid.NewGuid());
        var expiredRequirement = CreateDriverRequirement(
            chofer.Id, DriverRequirementStatus.Apto, DateOnly.FromDateTime(command.FechaLlegada).AddDays(-1));
        var harness = new Harness
        {
            VehicleRepository = new FakeVehicleRepository(vehicle),
            UserRepository = new FakeUserRepository(chofer),
            DriverRequirementRepository = new FakeDriverRequirementRepository([expiredRequirement]),
            EventoRepository = new FakeEventoRepository(CreateEvento())
        };

        await Assert.ThrowsAsync<ViajeNotHabilitadoException>(() =>
            harness.BuildHandler().Handle(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ChoferDateOverlap_ThrowsViajeNotHabilitado()
    {
        var vehicle = CreateVehicle();
        var chofer = CreateChofer();
        var harness = new Harness
        {
            VehicleRepository = new FakeVehicleRepository(vehicle),
            UserRepository = new FakeUserRepository(chofer),
            EventoRepository = new FakeEventoRepository(CreateEvento()),
            ViajeRepository = new FakeViajeRepository { ChoferOverlaps = [new Viaje { Id = Guid.NewGuid(), OrganizationId = OrganizationId, ChoferId = chofer.Id, VehicleId = Guid.NewGuid(), EventoId = Guid.NewGuid(), FechaSalida = DateTime.UtcNow, FechaLlegada = DateTime.UtcNow }] }
        };

        await Assert.ThrowsAsync<ViajeNotHabilitadoException>(() =>
            harness.BuildHandler().Handle(CreateCommand(vehicle.Id, chofer.Id, Guid.NewGuid()), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ChoferIdPointsAtNonChoferRole_ThrowsChoferNotFound()
    {
        var vehicle = CreateVehicle();
        var administrador = CreateChofer(role: Role.Administrador);
        var harness = new Harness
        {
            VehicleRepository = new FakeVehicleRepository(vehicle),
            UserRepository = new FakeUserRepository(administrador),
            EventoRepository = new FakeEventoRepository(CreateEvento())
        };

        await Assert.ThrowsAsync<ChoferNotFoundException>(() =>
            harness.BuildHandler().Handle(CreateCommand(vehicle.Id, administrador.Id, Guid.NewGuid()), TestContext.Current.CancellationToken));
    }

    // ---- Existence / IDOR ----

    [Fact]
    public async Task Handle_VehicleNotFound_ThrowsVehicleNotFound()
    {
        var harness = new Harness { VehicleRepository = new FakeVehicleRepository(null) };

        await Assert.ThrowsAsync<VehicleNotFoundException>(() =>
            harness.BuildHandler().Handle(CreateCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_VehicleBelongsToDifferentOrganization_ThrowsVehicleNotFound()
    {
        var otherOrgVehicle = CreateVehicle();
        otherOrgVehicle.OrganizationId = Guid.NewGuid();
        var harness = new Harness { VehicleRepository = new FakeVehicleRepository(otherOrgVehicle) };

        await Assert.ThrowsAsync<VehicleNotFoundException>(() =>
            harness.BuildHandler().Handle(CreateCommand(otherOrgVehicle.Id, Guid.NewGuid(), Guid.NewGuid()), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_EventoNotFound_ThrowsEventoNotFound()
    {
        var vehicle = CreateVehicle();
        var chofer = CreateChofer();
        var harness = new Harness
        {
            VehicleRepository = new FakeVehicleRepository(vehicle),
            UserRepository = new FakeUserRepository(chofer),
            EventoRepository = new FakeEventoRepository(null)
        };

        await Assert.ThrowsAsync<EventoNotFoundException>(() =>
            harness.BuildHandler().Handle(CreateCommand(vehicle.Id, chofer.Id, Guid.NewGuid()), TestContext.Current.CancellationToken));
    }

    // ---- Happy path ----

    [Fact]
    public async Task Handle_AllChecksPass_PersistsAggregateAndCallsDirectionsWithOrderedPoints()
    {
        var vehicle = CreateVehicle(capacity: 42);
        var chofer = CreateChofer();
        var evento = CreateEvento();
        var command = CreateCommand(vehicle.Id, chofer.Id, evento.Id);
        var harness = new Harness
        {
            VehicleRepository = new FakeVehicleRepository(vehicle),
            UserRepository = new FakeUserRepository(chofer),
            EventoRepository = new FakeEventoRepository(evento)
        };

        var result = await harness.BuildHandler().Handle(command, TestContext.Current.CancellationToken);

        // Directions called with the ordered points, origin first, destination last.
        Assert.NotNull(harness.DirectionsService.CalledWithPoints);
        Assert.Equal(2, harness.DirectionsService.CalledWithPoints!.Count);
        Assert.Equal((command.Stops[0].Latitud, command.Stops[0].Longitud), harness.DirectionsService.CalledWithPoints[0]);
        Assert.Equal((command.Stops[1].Latitud, command.Stops[1].Longitud), harness.DirectionsService.CalledWithPoints[1]);

        // Aggregate persisted atomically via one AddAsync call.
        Assert.NotNull(harness.ViajeRepository.AddedViaje);
        Assert.NotNull(harness.ViajeRepository.AddedRuta);
        Assert.Equal(2, harness.ViajeRepository.AddedStops.Count);

        var viaje = harness.ViajeRepository.AddedViaje!;
        Assert.Equal(42, viaje.Capacidad);
        Assert.Equal(42, viaje.AsientosDisponibles);
        Assert.Equal(ViajeEstado.Programado, viaje.Estado);
        Assert.Equal(OrganizationId, viaje.OrganizationId);

        var stops = harness.ViajeRepository.AddedStops.OrderBy(s => s.Orden).ToList();
        Assert.Equal(StopTipo.Origen, stops[0].Tipo);
        Assert.Equal(0, stops[0].Orden);
        Assert.Equal(StopTipo.Destino, stops[1].Tipo);
        Assert.Equal(1, stops[1].Orden);

        Assert.Equal(viaje.Id, result.Id);
        Assert.Equal(42, result.Capacidad);
        Assert.Equal(evento.Nombre, result.EventoNombre);
    }
}
