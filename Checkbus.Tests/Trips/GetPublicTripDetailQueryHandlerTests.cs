using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Queries;
using Checkbus.ApiService.Domain.Entities.Passengers;
using Checkbus.ApiService.Domain.Entities.Reservations;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Trips;

namespace Checkbus.Tests.Trips;

/// <summary>
/// Proves <see cref="GetPublicTripDetailQueryHandler"/> (odd/tasks/detalle-reserva-publica.md
/// T2) — same public, unauthenticated posture as <see cref="GetPublicTripSeatMapQueryHandler"/>:
/// gates purely on <c>Trip.Status == Publicado</c>, no <c>ICurrentUserService</c>, no org
/// scoping. Fake-repository style, matching <see cref="GetPublicTripSeatMapQueryHandlerTests"/>.
/// </summary>
public class GetPublicTripDetailQueryHandlerTests
{
    private sealed class FakeTripRepository(Trip? trip, Route? route = null, IReadOnlyList<Stop>? stops = null) : ITripRepository
    {
        public Task AddAsync(Trip trip, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripDetailQueryHandler.");

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(trip);

        public Task UpdateAsync(Trip trip, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripDetailQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripDetailQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(Guid vehicleId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripDetailQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(Guid driverId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripDetailQueryHandler.");

        public Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken) => Task.FromResult(route);

        public Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken)
            => Task.FromResult(stops ?? []);
    }

    private sealed class FakeVehicleRepository(Vehicle? vehicle) : IVehicleRepository
    {
        public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(vehicle);

        public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripDetailQueryHandler.");

        public Task<IReadOnlyList<Vehicle>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripDetailQueryHandler.");

        public Task<bool> PatentExistsAsync(string patent, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripDetailQueryHandler.");
    }

    private sealed class FakeReservationRepository(IReadOnlyList<Reservation> reservations) : IReservationRepository
    {
        public Task<IReadOnlyList<Reservation>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Reservation>>(reservations.Where(r => r.TripId == tripId).ToList());

        public Task CreateBatchAsync(
            IReadOnlyCollection<Passenger> passengers,
            IReadOnlyCollection<Reservation> newReservations,
            CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripDetailQueryHandler.");

        public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripDetailQueryHandler.");
    }

    private sealed class FakeOrganizationRepository(string? name) : IOrganizationRepository
    {
        public Task<string?> GetSlugByIdAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripDetailQueryHandler.");

        public Task<string?> GetNameByIdAsync(Guid organizationId, CancellationToken cancellationToken)
            => Task.FromResult(name);
    }

    private sealed class FakeEventRepository(Event? @event) : IEventRepository
    {
        public Task AddAsync(Event @event, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripDetailQueryHandler.");

        public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(@event);

        public Task UpdateAsync(Event @event, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripDetailQueryHandler.");

        public Task<IReadOnlyList<Event>> GetAllAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripDetailQueryHandler.");
    }

    private sealed class FakeLocationRepository(Dictionary<Guid, Location> locationsById) : ILocationRepository
    {
        public Task<Location?> FindByPlaceIdAsync(string placeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripDetailQueryHandler.");

        public Task AddAsync(Location location, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripDetailQueryHandler.");

        public Task<Location?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(locationsById.GetValueOrDefault(id));
    }

    private static Trip CreateTrip(TripStatus status = TripStatus.Publicado) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = Guid.NewGuid(),
        DriverId = Guid.NewGuid(),
        VehicleId = Guid.NewGuid(),
        EventId = Guid.NewGuid(),
        DepartureDate = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc),
        ArrivalDate = new DateTime(2026, 5, 1, 18, 0, 0, DateTimeKind.Utc),
        Capacity = 45,
        AvailableSeats = 45,
        Price = 15000m,
        Status = status
    };

    private static Vehicle CreateVehicle(Guid id, int capacity) => new()
    {
        Id = id,
        Brand = "Mercedes-Benz",
        Model = "O500",
        Year = 2020,
        Patent = "AB123CD",
        Capacity = capacity,
        Mileage = 1000,
        Status = VehicleStatus.Activo,
        OrganizationId = Guid.NewGuid(),
        OwnerType = VehicleOwnerType.Organizacion
    };

    private static GetPublicTripDetailQueryHandler CreateHandler(
        Trip? trip,
        Vehicle? vehicle = null,
        IReadOnlyList<Reservation>? reservations = null,
        string? organizationName = "Brahman",
        Event? @event = null,
        Route? route = null,
        IReadOnlyList<Stop>? stops = null,
        Dictionary<Guid, Location>? locationsById = null) =>
        new(
            new FakeTripRepository(trip, route, stops),
            new FakeVehicleRepository(vehicle),
            new FakeReservationRepository(reservations ?? []),
            new FakeOrganizationRepository(organizationName),
            new FakeEventRepository(@event),
            new FakeLocationRepository(locationsById ?? []));

    [Fact]
    public async Task Handle_PublishedTrip_ReturnsFullDetail()
    {
        var trip = CreateTrip();
        var vehicle = CreateVehicle(trip.VehicleId, capacity: 45);
        var @event = new Event
        {
            Id = trip.EventId,
            Name = "Coldplay en River",
            Type = EventType.Concierto,
            Date = trip.DepartureDate,
            LocationId = Guid.NewGuid(),
            ImageKey = "events/some-key.jpg"
        };
        var route = new Route { Id = Guid.NewGuid(), TripId = trip.Id, EstimatedDuration = TimeSpan.FromHours(2), DistanceKm = 50m };
        var location = new Location { Id = Guid.NewGuid(), Name = "Terminal", Address = "Av. Siempre Viva 123", PlaceId = "place-1", Latitude = -34.6, Longitude = -58.4 };
        var stop = new Stop { Id = Guid.NewGuid(), RouteId = route.Id, LocationId = location.Id, Order = 0, Type = StopType.Origen };
        var reservation = new Reservation { Id = Guid.NewGuid(), TripId = trip.Id, SeatId = Guid.NewGuid(), PassengerId = Guid.NewGuid(), Price = trip.Price };

        var handler = CreateHandler(
            trip,
            vehicle,
            reservations: [reservation],
            organizationName: "Brahman",
            @event: @event,
            route: route,
            stops: [stop],
            locationsById: new Dictionary<Guid, Location> { [location.Id] = location });

        var result = await handler.Handle(
            new GetPublicTripDetailQuery { TripId = trip.Id }, TestContext.Current.CancellationToken);

        Assert.Equal(trip.Id, result.TripId);
        Assert.Equal("Brahman", result.OrganizationName);
        Assert.Equal(trip.EventId, result.EventId);
        Assert.Equal("Coldplay en River", result.EventName);
        Assert.True(result.EventHasImage);
        Assert.Equal(trip.Price, result.Price);
        Assert.Equal(44, result.AvailableSeats); // 45 capacity - 1 reservation
        Assert.Equal(trip.DepartureDate, result.DepartureDate);
        Assert.Equal(trip.ArrivalDate, result.ArrivalDate);
        Assert.Single(result.Stops);
        Assert.Equal("Terminal", result.Stops[0].Name);
        Assert.Equal(StopType.Origen, result.Stops[0].Type);
    }

    [Fact]
    public async Task Handle_EventWithNoImage_ReturnsEventHasImageFalse()
    {
        var trip = CreateTrip();
        var @event = new Event
        {
            Id = trip.EventId,
            Name = "River Plate vs Boca",
            Type = EventType.Partido,
            Date = trip.DepartureDate,
            LocationId = Guid.NewGuid()
        };

        var handler = CreateHandler(trip, CreateVehicle(trip.VehicleId, 45), @event: @event);

        var result = await handler.Handle(
            new GetPublicTripDetailQuery { TripId = trip.Id }, TestContext.Current.CancellationToken);

        Assert.False(result.EventHasImage);
    }

    [Fact]
    public async Task Handle_NoRoute_ReturnsEmptyStops()
    {
        var trip = CreateTrip();

        var handler = CreateHandler(trip, CreateVehicle(trip.VehicleId, 45));

        var result = await handler.Handle(
            new GetPublicTripDetailQuery { TripId = trip.Id }, TestContext.Current.CancellationToken);

        Assert.Empty(result.Stops);
    }

    [Fact]
    public async Task Handle_TripNotFound_ThrowsTripNotFound()
    {
        var handler = CreateHandler(null);

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(new GetPublicTripDetailQuery { TripId = Guid.NewGuid() }, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(TripStatus.Programado)]
    [InlineData(TripStatus.EnProceso)]
    [InlineData(TripStatus.Completado)]
    [InlineData(TripStatus.Cancelado)]
    public async Task Handle_TripExistsButNotPublicado_ThrowsSameTripNotFound(TripStatus status)
    {
        // Indistinguishable from "truly doesn't exist" at the exception-type level — same
        // IDOR-safe rationale as GetPublicTripSeatMapQueryHandler.
        var trip = CreateTrip(status);
        var handler = CreateHandler(trip, CreateVehicle(trip.VehicleId, 45));

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(new GetPublicTripDetailQuery { TripId = trip.Id }, TestContext.Current.CancellationToken));
    }
}
