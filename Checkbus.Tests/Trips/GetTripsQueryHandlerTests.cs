using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Queries;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.Tests.Trips;

public class GetTripsQueryHandlerTests
{
    private sealed class FakeTripRepository(IReadOnlyList<Trip> trips) : ITripRepository
    {
        public Task AddAsync(Trip trip, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsQueryHandler.");

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => Task.FromResult(trips.Where(v => v.OrganizationId == organizationId).ToList() as IReadOnlyList<Trip>);

        public Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(Guid vehicleId, DateTime fechaSalida, DateTime fechaLlegada, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(Guid driverId, DateTime fechaSalida, DateTime fechaLlegada, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsQueryHandler.");

        public Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsQueryHandler.");

        public Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsQueryHandler.");
    }

    private sealed class FakeVehicleRepository(IReadOnlyList<Vehicle> vehicles) : IVehicleRepository
    {
        public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsQueryHandler.");

        public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsQueryHandler.");

        public Task<IReadOnlyList<Vehicle>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => Task.FromResult(vehicles);

        public Task<bool> PatentExistsAsync(string patent, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsQueryHandler.");
    }

    private sealed class FakeUserRepository(IReadOnlyList<User> users) : IUserRepository
    {
        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsQueryHandler.");

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsQueryHandler.");

        public Task AddAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsQueryHandler.");

        public Task UpdateAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsQueryHandler.");

        public Task<bool> DocumentNumberExistsInOrganizationAsync(string documentNumber, Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsQueryHandler.");

        public Task<IReadOnlyList<string>> FindEmailsByLocalPartPrefixAsync(string localPartPrefix, string domain, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsQueryHandler.");

        public Task<IReadOnlyList<User>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => Task.FromResult(users);
    }

    private sealed class FakeEventRepository(IReadOnlyList<Event> events) : IEventRepository
    {
        public Task AddAsync(Event @event, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsQueryHandler.");

        public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(events.FirstOrDefault(e => e.Id == id));

        public Task<IReadOnlyList<Event>> GetAllAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripsQueryHandler.");
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

    private static Trip CreateTrip(Guid organizationId, Guid vehicleId, Guid driverId, Guid eventId) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = organizationId,
        DriverId = driverId,
        VehicleId = vehicleId,
        EventId = eventId,
        DepartureDate = DateTime.UtcNow,
        ArrivalDate = DateTime.UtcNow.AddHours(8),
        Capacity = 45,
        AvailableSeats = 45,
        Price = 15000m
    };

    [Fact]
    public async Task Handle_ReturnsTripsForCallersOrganization_WithResolvedJoins()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(Guid.NewGuid(), organizationId);
        var driver = CreateDriver(Guid.NewGuid(), organizationId);
        var @event = CreateEvent(Guid.NewGuid());
        var trip = CreateTrip(organizationId, vehicle.Id, driver.Id, @event.Id);

        var handler = new GetTripsQueryHandler(
            new FakeTripRepository([trip]),
            new FakeVehicleRepository([vehicle]),
            new FakeUserRepository([driver]),
            new FakeEventRepository([@event]),
            new FakeCurrentUserService(organizationId));

        var result = await handler.Handle(new GetTripsQuery(), TestContext.Current.CancellationToken);

        var dto = Assert.Single(result);
        Assert.Equal(trip.Id, dto.Id);
        Assert.Equal(vehicle.Patent, dto.VehiclePatent);
        Assert.Equal(driver.Name, dto.DriverName);
        Assert.Equal(@event.Name, dto.EventName);
    }

    [Fact]
    public async Task Handle_NoTrips_ReturnsEmptyList()
    {
        var organizationId = Guid.NewGuid();
        var handler = new GetTripsQueryHandler(
            new FakeTripRepository([]),
            new FakeVehicleRepository([]),
            new FakeUserRepository([]),
            new FakeEventRepository([]),
            new FakeCurrentUserService(organizationId));

        var result = await handler.Handle(new GetTripsQuery(), TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var handler = new GetTripsQueryHandler(
            new FakeTripRepository([]),
            new FakeVehicleRepository([]),
            new FakeUserRepository([]),
            new FakeEventRepository([]),
            new FakeCurrentUserService(null));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new GetTripsQuery(), TestContext.Current.CancellationToken));
    }
}
