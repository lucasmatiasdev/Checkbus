using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Queries;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Exceptions.Trips;

namespace Checkbus.Tests.Trips;

public class GetTripQueryHandlerTests
{
    private sealed class FakeTripRepository(Trip? trip, Route? route, IReadOnlyList<Stop> stops) : ITripRepository
    {
        public Task AddAsync(Trip trip, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripQueryHandler.");

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(trip);

        public Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(Guid vehicleId, DateTime fechaSalida, DateTime fechaLlegada, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(Guid driverId, DateTime fechaSalida, DateTime fechaLlegada, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripQueryHandler.");

        public Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken) => Task.FromResult(route);

        public Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken) => Task.FromResult(stops);
    }

    private sealed class FakeVehicleRepository(Vehicle? vehicle) : IVehicleRepository
    {
        public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(vehicle);

        public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripQueryHandler.");

        public Task<IReadOnlyList<Vehicle>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripQueryHandler.");

        public Task<bool> PatentExistsAsync(string patent, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripQueryHandler.");
    }

    private sealed class FakeUserRepository(User? driver) : IUserRepository
    {
        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripQueryHandler.");

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(driver);

        public Task AddAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripQueryHandler.");

        public Task UpdateAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripQueryHandler.");

        public Task<bool> DocumentNumberExistsInOrganizationAsync(string documentNumber, Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripQueryHandler.");

        public Task<IReadOnlyList<string>> FindEmailsByLocalPartPrefixAsync(string localPartPrefix, string domain, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripQueryHandler.");

        public Task<IReadOnlyList<User>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripQueryHandler.");
    }

    private sealed class FakeEventRepository(Event? @event) : IEventRepository
    {
        public Task AddAsync(Event @event, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripQueryHandler.");

        public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(@event);

        public Task<IReadOnlyList<Event>> GetAllAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripQueryHandler.");
    }

    private sealed class FakeLocationRepository(Dictionary<Guid, Location> locationsById) : ILocationRepository
    {
        public Task<Location?> FindByPlaceIdAsync(string placeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripQueryHandler.");

        public Task AddAsync(Location location, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripQueryHandler.");

        public Task<Location?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(locationsById.GetValueOrDefault(id));
    }

    private sealed class FakeCurrentUserService(Guid? organizationId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.NewGuid();
        public Guid? OrganizationId => organizationId;
        public string? Role => "Planificador";
        public string? Email => "planificador@checkbus-demo.com";
    }

    private static GetTripQueryHandler BuildHandler(
        Guid organizationId,
        Trip? trip,
        Route? route = null,
        IReadOnlyList<Stop>? stops = null,
        Vehicle? vehicle = null,
        User? driver = null,
        Event? @event = null,
        Dictionary<Guid, Location>? locationsById = null) => new(
            new FakeTripRepository(trip, route, stops ?? []),
            new FakeVehicleRepository(vehicle),
            new FakeUserRepository(driver),
            new FakeEventRepository(@event),
            new FakeLocationRepository(locationsById ?? []),
            new FakeCurrentUserService(organizationId));

    [Fact]
    public async Task Handle_TripFound_ReturnsDetailWithRouteAndStops()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = new Vehicle { Id = Guid.NewGuid(), Brand = "Mercedes-Benz", Model = "O500", Patent = "AB123CD", OrganizationId = organizationId, OwnerType = VehicleOwnerType.Organizacion };
        var driver = new User { Id = Guid.NewGuid(), Name = "Juan", Surname = "Perez", Email = "j@p.com", PasswordHash = "x", DocumentNumber = "1", Role = Role.Chofer, OrganizationId = organizationId };
        var @event = new Event { Id = Guid.NewGuid(), Name = "Final Copa Argentina", Type = EventType.Partido, Date = DateTime.UtcNow, LocationId = Guid.NewGuid() };
        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            DriverId = driver.Id,
            VehicleId = vehicle.Id,
            EventId = @event.Id,
            DepartureDate = DateTime.UtcNow,
            ArrivalDate = DateTime.UtcNow.AddHours(8),
            Capacity = 45,
            AvailableSeats = 45,
            Price = 15000m
        };
        var route = new Route { Id = Guid.NewGuid(), TripId = trip.Id, EstimatedDuration = TimeSpan.FromHours(8), DistanceKm = 700m };
        var locationOrigin = new Location { Id = Guid.NewGuid(), Name = "Terminal Retiro", Address = "Dir 1", PlaceId = "place-1", Latitude = -34.5, Longitude = -58.3 };
        var locationDestination = new Location { Id = Guid.NewGuid(), Name = "Estadio Kempes", Address = "Dir 2", PlaceId = "place-2", Latitude = -31.3, Longitude = -64.2 };
        var stops = new List<Stop>
        {
            new() { Id = Guid.NewGuid(), RouteId = route.Id, LocationId = locationOrigin.Id, Order = 0, Type = StopType.Origen },
            new() { Id = Guid.NewGuid(), RouteId = route.Id, LocationId = locationDestination.Id, Order = 1, Type = StopType.Destino }
        };

        var handler = BuildHandler(
            organizationId, trip, route, stops, vehicle, driver, @event,
            new Dictionary<Guid, Location> { [locationOrigin.Id] = locationOrigin, [locationDestination.Id] = locationDestination });

        var result = await handler.Handle(new GetTripQuery { Id = trip.Id }, TestContext.Current.CancellationToken);

        Assert.Equal(trip.Id, result.Id);
        Assert.Equal(route.EstimatedDuration, result.Route.EstimatedDuration);
        Assert.Equal(route.DistanceKm, result.Route.DistanceKm);
        Assert.Equal(2, result.Route.Stops.Count);
        Assert.Equal("Terminal Retiro", result.Route.Stops[0].Name);
        Assert.Equal(StopType.Origen, result.Route.Stops[0].Type);
        Assert.Equal("Estadio Kempes", result.Route.Stops[1].Name);
        Assert.Equal(StopType.Destino, result.Route.Stops[1].Type);
    }

    [Fact]
    public async Task Handle_TripNotFound_ThrowsTripNotFound()
    {
        var handler = BuildHandler(Guid.NewGuid(), trip: null);

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(new GetTripQuery { Id = Guid.NewGuid() }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_TripBelongsToDifferentOrganization_ThrowsTripNotFound()
    {
        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            DriverId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            DepartureDate = DateTime.UtcNow,
            ArrivalDate = DateTime.UtcNow.AddHours(8)
        };

        var handler = BuildHandler(Guid.NewGuid(), trip);

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(new GetTripQuery { Id = trip.Id }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var handler = new GetTripQueryHandler(
            new FakeTripRepository(null, null, []),
            new FakeVehicleRepository(null),
            new FakeUserRepository(null),
            new FakeEventRepository(null),
            new FakeLocationRepository([]),
            new FakeCurrentUserService(null));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new GetTripQuery { Id = Guid.NewGuid() }, TestContext.Current.CancellationToken));
    }
}
