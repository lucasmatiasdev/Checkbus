using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Maps;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Commands;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Documents;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using Checkbus.ApiService.Domain.Exceptions.VehicleDocuments;

namespace Checkbus.Tests.Trips;

public class CreateTripCommandHandlerTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();

    private sealed class FakeVehicleRepository(Vehicle? vehicle) : IVehicleRepository
    {
        public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(vehicle);

        public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateTripCommandHandler.");

        public Task<IReadOnlyList<Vehicle>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateTripCommandHandler.");

        public Task<bool> PatentExistsAsync(string patent, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateTripCommandHandler.");
    }

    private sealed class FakeVehicleDocumentRepository(IReadOnlyList<VehicleDocument> documents) : IVehicleDocumentRepository
    {
        public Task<IReadOnlyList<VehicleDocument>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken)
            => Task.FromResult(documents);

        public Task AddRangeAsync(IEnumerable<VehicleDocument> documents, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateTripCommandHandler.");

        public Task AddAsync(VehicleDocument document, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateTripCommandHandler.");

        public Task UpdateAsync(VehicleDocument document, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateTripCommandHandler.");

        public Task<int> GetExpiringOrExpiredCountByOrganizationAsync(
            Guid organizationId, DateOnly expiringThresholdDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateTripCommandHandler.");
    }

    private sealed class FakeUserRepository(User? driver) : IUserRepository
    {
        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateTripCommandHandler.");

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(driver);

        public Task AddAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateTripCommandHandler.");

        public Task UpdateAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateTripCommandHandler.");

        public Task<bool> DocumentNumberExistsInOrganizationAsync(string documentNumber, Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateTripCommandHandler.");

        public Task<IReadOnlyList<string>> FindEmailsByLocalPartPrefixAsync(string localPartPrefix, string domain, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateTripCommandHandler.");

        public Task<IReadOnlyList<User>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateTripCommandHandler.");
    }

    private sealed class FakeDriverRequirementRepository(IReadOnlyList<DriverRequirement> requirements) : IDriverRequirementRepository
    {
        public Task<IReadOnlyList<DriverRequirement>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(requirements);

        public Task AddRangeAsync(IEnumerable<DriverRequirement> requirements, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateTripCommandHandler.");

        public Task UpdateAsync(DriverRequirement requirement, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateTripCommandHandler.");

        public Task<int> GetExpiringOrExpiredCountByOrganizationAsync(
            Guid organizationId, DateOnly expiringThresholdDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateTripCommandHandler.");
    }

    private sealed class FakeEventRepository(Event? @event) : IEventRepository
    {
        public Task AddAsync(Event @event, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateTripCommandHandler.");

        public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(@event);

        public Task<IReadOnlyList<Event>> GetAllAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateTripCommandHandler.");
    }

    private sealed class FakeLocationRepository : ILocationRepository
    {
        public List<Location> AddedLocations { get; } = [];

        public Task<Location?> FindByPlaceIdAsync(string placeId, CancellationToken cancellationToken)
            => Task.FromResult<Location?>(null);

        public Task AddAsync(Location location, CancellationToken cancellationToken)
        {
            AddedLocations.Add(location);
            return Task.CompletedTask;
        }

        public Task<Location?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateTripCommandHandler.");
    }

    private sealed class FakeTripRepository : ITripRepository
    {
        public IReadOnlyList<Trip> VehicleOverlaps { get; set; } = [];
        public IReadOnlyList<Trip> DriverOverlaps { get; set; } = [];

        public Trip? AddedTrip { get; private set; }
        public Route? AddedRoute { get; private set; }
        public IReadOnlyList<Stop> AddedStops { get; private set; } = [];

        public Task AddAsync(Trip trip, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken)
        {
            AddedTrip = trip;
            AddedRoute = route;
            AddedStops = stops.ToList();
            return Task.CompletedTask;
        }

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateTripCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateTripCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(
            Guid vehicleId, DateTime fechaSalida, DateTime fechaLlegada, CancellationToken cancellationToken)
            => Task.FromResult(VehicleOverlaps);

        public Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(
            Guid driverId, DateTime fechaSalida, DateTime fechaLlegada, CancellationToken cancellationToken)
            => Task.FromResult(DriverOverlaps);

        public Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateTripCommandHandler.");

        public Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateTripCommandHandler.");
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

    private static User CreateDriver(bool isActive = true, Role role = Role.Chofer) => new()
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

    private static Event CreateEvent() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Final Copa Argentina",
        Type = EventType.Partido,
        Date = new DateTime(2026, 5, 1, 20, 0, 0, DateTimeKind.Utc),
        LocationId = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static CreateTripCommand CreateCommand(Guid vehicleId, Guid driverId, Guid eventId) => new()
    {
        VehicleId = vehicleId,
        DriverId = driverId,
        EventId = eventId,
        DepartureDate = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc),
        ArrivalDate = new DateTime(2026, 5, 1, 18, 0, 0, DateTimeKind.Utc),
        Price = 15000m,
        Stops =
        [
            new CreateTripStopCommand { Name = "Terminal Retiro", Address = "Av. Dr. Jose Maria Ramos Mejia 1680", PlaceId = "place-retiro", Latitude = -34.5921, Longitude = -58.3747 },
            new CreateTripStopCommand { Name = "Estadio Mario Alberto Kempes", Address = "Av. Cardeñosa, Córdoba", PlaceId = "place-kempes", Latitude = -31.3333, Longitude = -64.2333 }
        ]
    };

    private sealed class Harness
    {
        public FakeVehicleRepository VehicleRepository { get; set; } = new(null);
        public FakeVehicleDocumentRepository VehicleDocumentRepository { get; set; } = new([]);
        public FakeUserRepository UserRepository { get; set; } = new(null);
        public FakeDriverRequirementRepository DriverRequirementRepository { get; set; } = new([]);
        public FakeEventRepository EventRepository { get; set; } = new(null);
        public FakeLocationRepository LocationRepository { get; set; } = new();
        public FakeTripRepository TripRepository { get; set; } = new();
        public FakeDirectionsService DirectionsService { get; set; } = new(new DirectionsResult(TimeSpan.FromHours(8), 700m));
        public Guid? OrganizationId { get; set; } = CreateTripCommandHandlerTests.OrganizationId;

        public CreateTripCommandHandler BuildHandler() => new(
            VehicleRepository,
            VehicleDocumentRepository,
            UserRepository,
            DriverRequirementRepository,
            EventRepository,
            LocationRepository,
            TripRepository,
            DirectionsService,
            new FakeCurrentUserService(OrganizationId));
    }

    // ---- Vehicle habilitación ----

    [Fact]
    public async Task Handle_InactiveVehicle_ThrowsTripNotEligible()
    {
        var vehicle = CreateVehicle(VehicleStatus.Inactivo);
        var driver = CreateDriver();
        var harness = new Harness
        {
            VehicleRepository = new FakeVehicleRepository(vehicle),
            UserRepository = new FakeUserRepository(driver),
            EventRepository = new FakeEventRepository(CreateEvent())
        };

        await Assert.ThrowsAsync<TripNotEligibleException>(() =>
            harness.BuildHandler().Handle(CreateCommand(vehicle.Id, driver.Id, Guid.NewGuid()), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_VehicleEnMantenimiento_IsNotRejected()
    {
        var vehicle = CreateVehicle(VehicleStatus.EnMantenimiento);
        var driver = CreateDriver();
        var harness = new Harness
        {
            VehicleRepository = new FakeVehicleRepository(vehicle),
            UserRepository = new FakeUserRepository(driver),
            EventRepository = new FakeEventRepository(CreateEvent())
        };

        var result = await harness.BuildHandler().Handle(
            CreateCommand(vehicle.Id, driver.Id, Guid.NewGuid()), TestContext.Current.CancellationToken);

        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task Handle_ExpiredVehicleDocument_ThrowsTripNotEligible()
    {
        var vehicle = CreateVehicle();
        var driver = CreateDriver();
        var command = CreateCommand(vehicle.Id, driver.Id, Guid.NewGuid());
        var expiredDocument = CreateVehicleDocument(
            vehicle.Id, VehicleDocumentStatus.Apto, DateOnly.FromDateTime(command.ArrivalDate).AddDays(-1));
        var harness = new Harness
        {
            VehicleRepository = new FakeVehicleRepository(vehicle),
            VehicleDocumentRepository = new FakeVehicleDocumentRepository([expiredDocument]),
            UserRepository = new FakeUserRepository(driver),
            EventRepository = new FakeEventRepository(CreateEvent())
        };

        await Assert.ThrowsAsync<TripNotEligibleException>(() =>
            harness.BuildHandler().Handle(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_VehicleDateOverlap_ThrowsTripNotEligible()
    {
        var vehicle = CreateVehicle();
        var driver = CreateDriver();
        var harness = new Harness
        {
            VehicleRepository = new FakeVehicleRepository(vehicle),
            UserRepository = new FakeUserRepository(driver),
            EventRepository = new FakeEventRepository(CreateEvent()),
            TripRepository = new FakeTripRepository { VehicleOverlaps = [new Trip { Id = Guid.NewGuid(), OrganizationId = OrganizationId, DriverId = Guid.NewGuid(), VehicleId = vehicle.Id, EventId = Guid.NewGuid(), DepartureDate = DateTime.UtcNow, ArrivalDate = DateTime.UtcNow }] }
        };

        await Assert.ThrowsAsync<TripNotEligibleException>(() =>
            harness.BuildHandler().Handle(CreateCommand(vehicle.Id, driver.Id, Guid.NewGuid()), TestContext.Current.CancellationToken));
    }

    // ---- Driver eligibility ----

    [Fact]
    public async Task Handle_InactiveDriverAccount_ThrowsTripNotEligible()
    {
        var vehicle = CreateVehicle();
        var driver = CreateDriver(isActive: false);
        var harness = new Harness
        {
            VehicleRepository = new FakeVehicleRepository(vehicle),
            UserRepository = new FakeUserRepository(driver),
            EventRepository = new FakeEventRepository(CreateEvent())
        };

        await Assert.ThrowsAsync<TripNotEligibleException>(() =>
            harness.BuildHandler().Handle(CreateCommand(vehicle.Id, driver.Id, Guid.NewGuid()), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ExpiredDriverRequirement_ThrowsTripNotEligible()
    {
        var vehicle = CreateVehicle();
        var driver = CreateDriver();
        var command = CreateCommand(vehicle.Id, driver.Id, Guid.NewGuid());
        var expiredRequirement = CreateDriverRequirement(
            driver.Id, DriverRequirementStatus.Apto, DateOnly.FromDateTime(command.ArrivalDate).AddDays(-1));
        var harness = new Harness
        {
            VehicleRepository = new FakeVehicleRepository(vehicle),
            UserRepository = new FakeUserRepository(driver),
            DriverRequirementRepository = new FakeDriverRequirementRepository([expiredRequirement]),
            EventRepository = new FakeEventRepository(CreateEvent())
        };

        await Assert.ThrowsAsync<TripNotEligibleException>(() =>
            harness.BuildHandler().Handle(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_DriverDateOverlap_ThrowsTripNotEligible()
    {
        var vehicle = CreateVehicle();
        var driver = CreateDriver();
        var harness = new Harness
        {
            VehicleRepository = new FakeVehicleRepository(vehicle),
            UserRepository = new FakeUserRepository(driver),
            EventRepository = new FakeEventRepository(CreateEvent()),
            TripRepository = new FakeTripRepository { DriverOverlaps = [new Trip { Id = Guid.NewGuid(), OrganizationId = OrganizationId, DriverId = driver.Id, VehicleId = Guid.NewGuid(), EventId = Guid.NewGuid(), DepartureDate = DateTime.UtcNow, ArrivalDate = DateTime.UtcNow }] }
        };

        await Assert.ThrowsAsync<TripNotEligibleException>(() =>
            harness.BuildHandler().Handle(CreateCommand(vehicle.Id, driver.Id, Guid.NewGuid()), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_DriverIdPointsAtNonChoferRole_ThrowsDriverNotFound()
    {
        var vehicle = CreateVehicle();
        var administrador = CreateDriver(role: Role.Administrador);
        var harness = new Harness
        {
            VehicleRepository = new FakeVehicleRepository(vehicle),
            UserRepository = new FakeUserRepository(administrador),
            EventRepository = new FakeEventRepository(CreateEvent())
        };

        await Assert.ThrowsAsync<DriverNotFoundException>(() =>
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
    public async Task Handle_EventNotFound_ThrowsEventNotFound()
    {
        var vehicle = CreateVehicle();
        var driver = CreateDriver();
        var harness = new Harness
        {
            VehicleRepository = new FakeVehicleRepository(vehicle),
            UserRepository = new FakeUserRepository(driver),
            EventRepository = new FakeEventRepository(null)
        };

        await Assert.ThrowsAsync<EventNotFoundException>(() =>
            harness.BuildHandler().Handle(CreateCommand(vehicle.Id, driver.Id, Guid.NewGuid()), TestContext.Current.CancellationToken));
    }

    // ---- Happy path ----

    [Fact]
    public async Task Handle_AllChecksPass_PersistsAggregateAndCallsDirectionsWithOrderedPoints()
    {
        var vehicle = CreateVehicle(capacity: 42);
        var driver = CreateDriver();
        var @event = CreateEvent();
        var command = CreateCommand(vehicle.Id, driver.Id, @event.Id);
        var harness = new Harness
        {
            VehicleRepository = new FakeVehicleRepository(vehicle),
            UserRepository = new FakeUserRepository(driver),
            EventRepository = new FakeEventRepository(@event)
        };

        var result = await harness.BuildHandler().Handle(command, TestContext.Current.CancellationToken);

        // Directions called with the ordered points, origin first, destination last.
        Assert.NotNull(harness.DirectionsService.CalledWithPoints);
        Assert.Equal(2, harness.DirectionsService.CalledWithPoints!.Count);
        Assert.Equal((command.Stops[0].Latitude, command.Stops[0].Longitude), harness.DirectionsService.CalledWithPoints[0]);
        Assert.Equal((command.Stops[1].Latitude, command.Stops[1].Longitude), harness.DirectionsService.CalledWithPoints[1]);

        // Aggregate persisted atomically via one AddAsync call.
        Assert.NotNull(harness.TripRepository.AddedTrip);
        Assert.NotNull(harness.TripRepository.AddedRoute);
        Assert.Equal(2, harness.TripRepository.AddedStops.Count);

        var trip = harness.TripRepository.AddedTrip!;
        Assert.Equal(42, trip.Capacity);
        Assert.Equal(42, trip.AvailableSeats);
        Assert.Equal(TripStatus.Programado, trip.Status);
        Assert.Equal(OrganizationId, trip.OrganizationId);

        var stops = harness.TripRepository.AddedStops.OrderBy(s => s.Order).ToList();
        Assert.Equal(StopType.Origen, stops[0].Type);
        Assert.Equal(0, stops[0].Order);
        Assert.Equal(StopType.Destino, stops[1].Type);
        Assert.Equal(1, stops[1].Order);

        Assert.Equal(trip.Id, result.Id);
        Assert.Equal(42, result.Capacity);
        Assert.Equal(@event.Name, result.EventName);
    }
}
