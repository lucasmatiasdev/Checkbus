using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Queries;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.Tests.Boarding;

/// <summary>
/// Proves <see cref="GetDriverTripsQueryHandler"/> — "Mis viajes" (odd/tasks/reservas-publicas.md
/// Fase 3 / A2) — scopes exclusively to the caller's own DriverId (confirmed to be the
/// authenticated Chofer's own User.Id, there is no separate Driver entity) and only returns
/// Publicado trips, ordered by DepartureDate. Fake-repository style, matching
/// <see cref="GetTripsQueryHandlerTests"/>.
/// </summary>
public class GetDriverTripsQueryHandlerTests
{
    private sealed class FakeTripRepository(IReadOnlyList<Trip> trips) : ITripRepository
    {
        public Task AddAsync(Trip trip, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetDriverTripsQueryHandler.");

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetDriverTripsQueryHandler.");

        public Task UpdateAsync(Trip trip, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetDriverTripsQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => Task.FromResult(trips.Where(t => t.OrganizationId == organizationId).ToList() as IReadOnlyList<Trip>);

        public Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(Guid vehicleId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetDriverTripsQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(Guid driverId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetDriverTripsQueryHandler.");

        public Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetDriverTripsQueryHandler.");

        public Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetDriverTripsQueryHandler.");

        public Task UpdateStopDepartureTimesAsync(
            IReadOnlyDictionary<Guid, DateTime> departureTimesByStopId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetDriverTripsQueryHandler.");

        public Task<bool> SlugExistsAsync(Guid organizationId, string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetDriverTripsQueryHandler.");

        public Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetDriverTripsQueryHandler.");
    }

    private sealed class FakeVehicleRepository(IReadOnlyList<Vehicle> vehicles) : IVehicleRepository
    {
        public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetDriverTripsQueryHandler.");

        public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetDriverTripsQueryHandler.");

        public Task<IReadOnlyList<Vehicle>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => Task.FromResult(vehicles);

        public Task<bool> PatentExistsAsync(string patent, Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetDriverTripsQueryHandler.");
    }

    private sealed class FakeUserRepository(User? driver) : IUserRepository
    {
        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetDriverTripsQueryHandler.");

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(driver is not null && driver.Id == id ? driver : null);

        public Task AddAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetDriverTripsQueryHandler.");

        public Task UpdateAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetDriverTripsQueryHandler.");

        public Task<bool> DocumentNumberExistsInOrganizationAsync(string documentNumber, Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetDriverTripsQueryHandler.");

        public Task<IReadOnlyList<string>> FindEmailsByLocalPartPrefixAsync(string localPartPrefix, string domain, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetDriverTripsQueryHandler.");

        public Task<IReadOnlyList<User>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetDriverTripsQueryHandler.");
    }

    private sealed class FakeEventRepository(IReadOnlyList<Event> events) : IEventRepository
    {
        public Task AddAsync(Event @event, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetDriverTripsQueryHandler.");

        public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(events.FirstOrDefault(e => e.Id == id));

        public Task<IReadOnlyList<Event>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetDriverTripsQueryHandler.");

        public Task UpdateAsync(Event @event, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetDriverTripsQueryHandler.");
    }

    private sealed class FakeCurrentUserService(Guid? organizationId, Guid? userId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => userId;
        public Guid? OrganizationId => organizationId;
        public string? Role => "Chofer";
        public string? Email => "chofer@checkbus-demo.com";
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
        Email = "juan.perez@checkbus-demo.com",
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
        Guid organizationId, Guid vehicleId, Guid driverId, Guid eventId,
        TripStatus status = TripStatus.Publicado, DateTime? departureDate = null) => new()
    {
        Id = Guid.NewGuid(),
        Slug = Guid.NewGuid().ToString(),
        OrganizationId = organizationId,
        DriverId = driverId,
        VehicleId = vehicleId,
        EventId = eventId,
        DepartureDate = departureDate ?? DateTime.UtcNow,
        ArrivalDate = (departureDate ?? DateTime.UtcNow).AddHours(8),
        Capacity = 45,
        AvailableSeats = 45,
        Price = 15000m,
        Status = status
    };

    [Fact]
    public async Task Handle_ReturnsOnlyCallersOwnPublicadoTrips_OrderedByDepartureDate()
    {
        var organizationId = Guid.NewGuid();
        var driver = CreateDriver(Guid.NewGuid(), organizationId);
        var otherDriver = CreateDriver(Guid.NewGuid(), organizationId);
        var vehicle = CreateVehicle(Guid.NewGuid(), organizationId);
        var @event = CreateEvent(Guid.NewGuid());

        var laterTrip = CreateTrip(organizationId, vehicle.Id, driver.Id, @event.Id, departureDate: DateTime.UtcNow.AddDays(2));
        var earlierTrip = CreateTrip(organizationId, vehicle.Id, driver.Id, @event.Id, departureDate: DateTime.UtcNow.AddDays(1));
        var otherDriversTrip = CreateTrip(organizationId, vehicle.Id, otherDriver.Id, @event.Id);
        var notPublishedTrip = CreateTrip(organizationId, vehicle.Id, driver.Id, @event.Id, TripStatus.Programado);

        var handler = new GetDriverTripsQueryHandler(
            new FakeTripRepository([laterTrip, earlierTrip, otherDriversTrip, notPublishedTrip]),
            new FakeVehicleRepository([vehicle]),
            new FakeUserRepository(driver),
            new FakeEventRepository([@event]),
            new FakeCurrentUserService(organizationId, driver.Id));

        var result = await handler.Handle(new GetDriverTripsQuery(), TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.Equal([earlierTrip.Id, laterTrip.Id], result.Select(t => t.Id).ToList());
        Assert.All(result, dto => Assert.Equal(driver.Id, dto.DriverId));
    }

    [Fact]
    public async Task Handle_NoTripsForCaller_ReturnsEmptyList()
    {
        var organizationId = Guid.NewGuid();
        var driver = CreateDriver(Guid.NewGuid(), organizationId);

        var handler = new GetDriverTripsQueryHandler(
            new FakeTripRepository([]),
            new FakeVehicleRepository([]),
            new FakeUserRepository(driver),
            new FakeEventRepository([]),
            new FakeCurrentUserService(organizationId, driver.Id));

        var result = await handler.Handle(new GetDriverTripsQuery(), TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var handler = new GetDriverTripsQueryHandler(
            new FakeTripRepository([]),
            new FakeVehicleRepository([]),
            new FakeUserRepository(null),
            new FakeEventRepository([]),
            new FakeCurrentUserService(null, Guid.NewGuid()));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new GetDriverTripsQuery(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_NullUserIdClaim_ThrowsInvalidOperationException()
    {
        var handler = new GetDriverTripsQueryHandler(
            new FakeTripRepository([]),
            new FakeVehicleRepository([]),
            new FakeUserRepository(null),
            new FakeEventRepository([]),
            new FakeCurrentUserService(Guid.NewGuid(), null));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new GetDriverTripsQuery(), TestContext.Current.CancellationToken));
    }
}
