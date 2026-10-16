using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Queries;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.Tests.Trips;

public class GetTripsReportQueryHandlerTests
{
    private sealed class FakeTripRepository(IReadOnlyList<Trip> trips) : ITripRepository
    {
        public Task AddAsync(Trip trip, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsReportQueryHandler.");

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsReportQueryHandler.");

        public Task UpdateAsync(Trip trip, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsReportQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => Task.FromResult(trips.Where(t => t.OrganizationId == organizationId).ToList() as IReadOnlyList<Trip>);

        public Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(Guid vehicleId, DateTime fechaSalida, DateTime fechaLlegada, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsReportQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(Guid driverId, DateTime fechaSalida, DateTime fechaLlegada, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsReportQueryHandler.");

        public Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsReportQueryHandler.");

        public Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsReportQueryHandler.");

        public Task UpdateStopDepartureTimesAsync(
            IReadOnlyDictionary<Guid, DateTime> departureTimesByStopId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsReportQueryHandler.");

        public Task<bool> SlugExistsAsync(Guid organizationId, string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsReportQueryHandler.");

        public Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsReportQueryHandler.");
    }

    private sealed class FakeVehicleRepository(IReadOnlyList<Vehicle> vehicles) : IVehicleRepository
    {
        public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsReportQueryHandler.");

        public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsReportQueryHandler.");

        public Task<IReadOnlyList<Vehicle>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => Task.FromResult(vehicles);

        public Task<bool> PatentExistsAsync(string patent, Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsReportQueryHandler.");
    }

    private sealed class FakeUserRepository(IReadOnlyList<User> users) : IUserRepository
    {
        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsReportQueryHandler.");

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsReportQueryHandler.");

        public Task AddAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsReportQueryHandler.");

        public Task UpdateAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsReportQueryHandler.");

        public Task<bool> DocumentNumberExistsInOrganizationAsync(string documentNumber, Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsReportQueryHandler.");

        public Task<IReadOnlyList<string>> FindEmailsByLocalPartPrefixAsync(string localPartPrefix, string domain, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsReportQueryHandler.");

        public Task<IReadOnlyList<User>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => Task.FromResult(users);
    }

    private sealed class FakeEventRepository(IReadOnlyList<Event> events) : IEventRepository
    {
        public Task AddAsync(Event @event, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsReportQueryHandler.");

        public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(events.FirstOrDefault(e => e.Id == id));

        public Task<IReadOnlyList<Event>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsReportQueryHandler.");

        public Task UpdateAsync(Event @event, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsReportQueryHandler.");
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

    private static User CreateDriver(Guid id, Guid organizationId) => new()
    {
        Id = id,
        Name = "Juan",
        Surname = "Perez",
        Email = $"{id}@checkbus-demo.com",
        PasswordHash = "hashed-password",
        DocumentNumber = "30123456",
        Role = Role.Chofer,
        OrganizationId = organizationId,
        IsActive = true
    };

    private static Event CreateEvent(Guid id) => new()
    {
        Id = id,
        Name = "Final Copa Argentina",
        Type = EventType.Partido,
        Date = DateTime.UtcNow,
        LocationId = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static Trip CreateTrip(
        Guid organizationId,
        Guid vehicleId,
        Guid driverId,
        Guid eventId,
        DateTime? departureDate = null,
        TripStatus status = TripStatus.Programado,
        int capacity = 45,
        int availableSeats = 45,
        decimal price = 15000m) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = organizationId,
        DriverId = driverId,
        VehicleId = vehicleId,
        EventId = eventId,
        Slug = Guid.NewGuid().ToString(),
        DepartureDate = departureDate ?? DateTime.UtcNow,
        ArrivalDate = (departureDate ?? DateTime.UtcNow).AddHours(8),
        Capacity = capacity,
        AvailableSeats = availableSeats,
        Price = price,
        Status = status
    };

    private static GetTripsReportQueryHandler CreateHandler(
        IReadOnlyList<Trip> trips,
        IReadOnlyList<Vehicle> vehicles,
        IReadOnlyList<User> users,
        IReadOnlyList<Event> events,
        Guid? organizationId)
        => new(
            new FakeTripRepository(trips),
            new FakeVehicleRepository(vehicles),
            new FakeUserRepository(users),
            new FakeEventRepository(events),
            new FakeCurrentUserService(organizationId));

    [Fact]
    public async Task Handle_NoFilters_ReturnsAllTripsWithStatistics()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(Guid.NewGuid(), organizationId);
        var driver = CreateDriver(Guid.NewGuid(), organizationId);
        var @event = CreateEvent(Guid.NewGuid());
        var trip1 = CreateTrip(organizationId, vehicle.Id, driver.Id, @event.Id, capacity: 45, availableSeats: 40, price: 1000m);
        var trip2 = CreateTrip(organizationId, vehicle.Id, driver.Id, @event.Id, capacity: 45, availableSeats: 45, price: 500m, status: TripStatus.Completado);

        var handler = CreateHandler([trip1, trip2], [vehicle], [driver], [@event], organizationId);

        var result = await handler.Handle(new GetTripsReportQuery(), TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Trips.Count);
        Assert.Equal(2, result.Statistics.TotalTrips);
        Assert.Equal(5, result.Statistics.TotalPassengers); // (45-40) + (45-45)
        Assert.Equal(5000m, result.Statistics.EstimatedRevenue); // 5*1000 + 0*500
        Assert.Equal(1, result.Statistics.TripsByStatus[TripStatus.Programado]);
        Assert.Equal(1, result.Statistics.TripsByStatus[TripStatus.Completado]);
    }

    [Fact]
    public async Task Handle_DateFromFilter_ExcludesEarlierTrips()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(Guid.NewGuid(), organizationId);
        var driver = CreateDriver(Guid.NewGuid(), organizationId);
        var @event = CreateEvent(Guid.NewGuid());
        var earlier = CreateTrip(organizationId, vehicle.Id, driver.Id, @event.Id, departureDate: new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc));
        var later = CreateTrip(organizationId, vehicle.Id, driver.Id, @event.Id, departureDate: new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc));

        var handler = CreateHandler([earlier, later], [vehicle], [driver], [@event], organizationId);

        var result = await handler.Handle(
            new GetTripsReportQuery { DateFrom = new DateOnly(2026, 3, 1) },
            TestContext.Current.CancellationToken);

        var dto = Assert.Single(result.Trips);
        Assert.Equal(later.Id, dto.Id);
    }

    [Fact]
    public async Task Handle_DateToFilter_ExcludesLaterTrips()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(Guid.NewGuid(), organizationId);
        var driver = CreateDriver(Guid.NewGuid(), organizationId);
        var @event = CreateEvent(Guid.NewGuid());
        var earlier = CreateTrip(organizationId, vehicle.Id, driver.Id, @event.Id, departureDate: new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc));
        var later = CreateTrip(organizationId, vehicle.Id, driver.Id, @event.Id, departureDate: new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc));

        var handler = CreateHandler([earlier, later], [vehicle], [driver], [@event], organizationId);

        var result = await handler.Handle(
            new GetTripsReportQuery { DateTo = new DateOnly(2026, 3, 1) },
            TestContext.Current.CancellationToken);

        var dto = Assert.Single(result.Trips);
        Assert.Equal(earlier.Id, dto.Id);
    }

    [Fact]
    public async Task Handle_DateRangeFilter_ReturnsOnlyWithinRange()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(Guid.NewGuid(), organizationId);
        var driver = CreateDriver(Guid.NewGuid(), organizationId);
        var @event = CreateEvent(Guid.NewGuid());
        var before = CreateTrip(organizationId, vehicle.Id, driver.Id, @event.Id, departureDate: new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc));
        var within = CreateTrip(organizationId, vehicle.Id, driver.Id, @event.Id, departureDate: new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc));
        var after = CreateTrip(organizationId, vehicle.Id, driver.Id, @event.Id, departureDate: new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc));

        var handler = CreateHandler([before, within, after], [vehicle], [driver], [@event], organizationId);

        var result = await handler.Handle(
            new GetTripsReportQuery { DateFrom = new DateOnly(2026, 2, 1), DateTo = new DateOnly(2026, 4, 1) },
            TestContext.Current.CancellationToken);

        var dto = Assert.Single(result.Trips);
        Assert.Equal(within.Id, dto.Id);
    }

    [Fact]
    public async Task Handle_StatusFilter_ReturnsOnlyMatchingStatus()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(Guid.NewGuid(), organizationId);
        var driver = CreateDriver(Guid.NewGuid(), organizationId);
        var @event = CreateEvent(Guid.NewGuid());
        var programado = CreateTrip(organizationId, vehicle.Id, driver.Id, @event.Id, status: TripStatus.Programado);
        var completado = CreateTrip(organizationId, vehicle.Id, driver.Id, @event.Id, status: TripStatus.Completado);

        var handler = CreateHandler([programado, completado], [vehicle], [driver], [@event], organizationId);

        var result = await handler.Handle(
            new GetTripsReportQuery { Status = TripStatus.Completado },
            TestContext.Current.CancellationToken);

        var dto = Assert.Single(result.Trips);
        Assert.Equal(completado.Id, dto.Id);
    }

    [Fact]
    public async Task Handle_DriverIdFilter_ReturnsOnlyMatchingDriver()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(Guid.NewGuid(), organizationId);
        var driver1 = CreateDriver(Guid.NewGuid(), organizationId);
        var driver2 = CreateDriver(Guid.NewGuid(), organizationId);
        var @event = CreateEvent(Guid.NewGuid());
        var trip1 = CreateTrip(organizationId, vehicle.Id, driver1.Id, @event.Id);
        var trip2 = CreateTrip(organizationId, vehicle.Id, driver2.Id, @event.Id);

        var handler = CreateHandler([trip1, trip2], [vehicle], [driver1, driver2], [@event], organizationId);

        var result = await handler.Handle(
            new GetTripsReportQuery { DriverId = driver2.Id },
            TestContext.Current.CancellationToken);

        var dto = Assert.Single(result.Trips);
        Assert.Equal(trip2.Id, dto.Id);
    }

    [Fact]
    public async Task Handle_VehicleIdFilter_ReturnsOnlyMatchingVehicle()
    {
        var organizationId = Guid.NewGuid();
        var vehicle1 = CreateVehicle(Guid.NewGuid(), organizationId);
        var vehicle2 = CreateVehicle(Guid.NewGuid(), organizationId);
        var driver = CreateDriver(Guid.NewGuid(), organizationId);
        var @event = CreateEvent(Guid.NewGuid());
        var trip1 = CreateTrip(organizationId, vehicle1.Id, driver.Id, @event.Id);
        var trip2 = CreateTrip(organizationId, vehicle2.Id, driver.Id, @event.Id);

        var handler = CreateHandler([trip1, trip2], [vehicle1, vehicle2], [driver], [@event], organizationId);

        var result = await handler.Handle(
            new GetTripsReportQuery { VehicleId = vehicle2.Id },
            TestContext.Current.CancellationToken);

        var dto = Assert.Single(result.Trips);
        Assert.Equal(trip2.Id, dto.Id);
    }

    [Fact]
    public async Task Handle_CombinedFilters_ReturnsOnlyMatchingAll()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(Guid.NewGuid(), organizationId);
        var driver1 = CreateDriver(Guid.NewGuid(), organizationId);
        var driver2 = CreateDriver(Guid.NewGuid(), organizationId);
        var @event = CreateEvent(Guid.NewGuid());
        var match = CreateTrip(organizationId, vehicle.Id, driver1.Id, @event.Id, status: TripStatus.Completado);
        var wrongDriver = CreateTrip(organizationId, vehicle.Id, driver2.Id, @event.Id, status: TripStatus.Completado);
        var wrongStatus = CreateTrip(organizationId, vehicle.Id, driver1.Id, @event.Id, status: TripStatus.Programado);

        var handler = CreateHandler([match, wrongDriver, wrongStatus], [vehicle], [driver1, driver2], [@event], organizationId);

        var result = await handler.Handle(
            new GetTripsReportQuery { DriverId = driver1.Id, Status = TripStatus.Completado },
            TestContext.Current.CancellationToken);

        var dto = Assert.Single(result.Trips);
        Assert.Equal(match.Id, dto.Id);
    }

    [Fact]
    public async Task Handle_NoMatches_ReturnsEmptyListAndZeroStatistics()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(Guid.NewGuid(), organizationId);
        var driver = CreateDriver(Guid.NewGuid(), organizationId);
        var @event = CreateEvent(Guid.NewGuid());
        var trip = CreateTrip(organizationId, vehicle.Id, driver.Id, @event.Id, status: TripStatus.Programado);

        var handler = CreateHandler([trip], [vehicle], [driver], [@event], organizationId);

        var result = await handler.Handle(
            new GetTripsReportQuery { Status = TripStatus.Cancelado },
            TestContext.Current.CancellationToken);

        Assert.Empty(result.Trips);
        Assert.Equal(0, result.Statistics.TotalTrips);
        Assert.Equal(0, result.Statistics.TotalPassengers);
        Assert.Equal(0m, result.Statistics.EstimatedRevenue);
        Assert.Empty(result.Statistics.TripsByStatus);
    }

    [Fact]
    public async Task Handle_OtherOrganizationTrips_NeverIncluded()
    {
        var organizationId = Guid.NewGuid();
        var otherOrganizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(Guid.NewGuid(), organizationId);
        var driver = CreateDriver(Guid.NewGuid(), organizationId);
        var @event = CreateEvent(Guid.NewGuid());
        var ownTrip = CreateTrip(organizationId, vehicle.Id, driver.Id, @event.Id);
        var otherVehicle = CreateVehicle(Guid.NewGuid(), otherOrganizationId);
        var otherDriver = CreateDriver(Guid.NewGuid(), otherOrganizationId);
        var otherTrip = CreateTrip(otherOrganizationId, otherVehicle.Id, otherDriver.Id, @event.Id);

        var handler = CreateHandler([ownTrip, otherTrip], [vehicle], [driver], [@event], organizationId);

        var result = await handler.Handle(new GetTripsReportQuery(), TestContext.Current.CancellationToken);

        var dto = Assert.Single(result.Trips);
        Assert.Equal(ownTrip.Id, dto.Id);
        Assert.Equal(1, result.Statistics.TotalTrips);
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var handler = CreateHandler([], [], [], [], null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new GetTripsReportQuery(), TestContext.Current.CancellationToken));
    }
}
