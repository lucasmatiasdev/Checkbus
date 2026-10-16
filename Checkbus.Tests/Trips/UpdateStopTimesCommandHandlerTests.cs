using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Commands;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Exceptions.Trips;

namespace Checkbus.Tests.Trips;

/// <summary>
/// Proves <see cref="UpdateStopTimesCommandHandler"/> (odd/tasks/mejoras-detalle-viaje.md Fase
/// D3): org-ownership IDOR-safe gate (same as <see cref="GetTripQueryHandler"/>), rejection of a
/// StopId that doesn't belong to the Trip's own Route, and the happy path's batch update call.
/// </summary>
public class UpdateStopTimesCommandHandlerTests
{
    private sealed class FakeTripRepository(Trip? trip, Route? route, IReadOnlyList<Stop> stops) : ITripRepository
    {
        public IReadOnlyDictionary<Guid, DateTime>? UpdatedDepartureTimesByStopId { get; private set; }

        public Task AddAsync(Trip trip, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateStopTimesCommandHandler.");

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(trip);

        public Task UpdateAsync(Trip trip, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateStopTimesCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateStopTimesCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(Guid vehicleId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateStopTimesCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(Guid driverId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateStopTimesCommandHandler.");

        public Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken) => Task.FromResult(route);

        public Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken) => Task.FromResult(stops);

        public Task UpdateStopDepartureTimesAsync(
            IReadOnlyDictionary<Guid, DateTime> departureTimesByStopId, CancellationToken cancellationToken)
        {
            UpdatedDepartureTimesByStopId = departureTimesByStopId;
            return Task.CompletedTask;
        }

        public Task<bool> SlugExistsAsync(Guid organizationId, string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateStopTimesCommandHandler.");

        public Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateStopTimesCommandHandler.");
    }

    private sealed class FakeCurrentUserService(Guid? organizationId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.NewGuid();
        public Guid? OrganizationId => organizationId;
        public string? Role => "Planificador";
        public string? Email => "planificador@checkbus-demo.com";
    }

    private static Trip CreateTrip(Guid organizationId) => new()
    {
        Id = Guid.NewGuid(),
        Slug = Guid.NewGuid().ToString(),
        OrganizationId = organizationId,
        DriverId = Guid.NewGuid(),
        VehicleId = Guid.NewGuid(),
        EventId = Guid.NewGuid(),
        DepartureDate = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc),
        ArrivalDate = new DateTime(2026, 5, 1, 18, 0, 0, DateTimeKind.Utc)
    };

    [Fact]
    public async Task Handle_TripNotFound_ThrowsTripNotFound()
    {
        var handler = new UpdateStopTimesCommandHandler(
            new FakeTripRepository(null, null, []),
            new FakeCurrentUserService(Guid.NewGuid()));

        var command = new UpdateStopTimesCommand
        {
            TripId = Guid.NewGuid(),
            Stops = [new StopDepartureTimeUpdate { StopId = Guid.NewGuid(), DepartureTime = DateTime.UtcNow }]
        };

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_TripBelongsToDifferentOrganization_ThrowsTripNotFound()
    {
        var trip = CreateTrip(Guid.NewGuid());
        var handler = new UpdateStopTimesCommandHandler(
            new FakeTripRepository(trip, null, []),
            new FakeCurrentUserService(Guid.NewGuid()));

        var command = new UpdateStopTimesCommand
        {
            TripId = trip.Id,
            Stops = [new StopDepartureTimeUpdate { StopId = Guid.NewGuid(), DepartureTime = DateTime.UtcNow }]
        };

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_StopIdNotInTripRoute_ThrowsTripNotFound()
    {
        var organizationId = Guid.NewGuid();
        var trip = CreateTrip(organizationId);
        var route = new Route { Id = Guid.NewGuid(), TripId = trip.Id, EstimatedDuration = TimeSpan.FromHours(8), DistanceKm = 700m };
        var realStop = new Stop { Id = Guid.NewGuid(), RouteId = route.Id, LocationId = Guid.NewGuid(), Order = 0, Type = Checkbus.ApiService.Domain.Enums.StopType.Origen };

        var handler = new UpdateStopTimesCommandHandler(
            new FakeTripRepository(trip, route, [realStop]),
            new FakeCurrentUserService(organizationId));

        var command = new UpdateStopTimesCommand
        {
            TripId = trip.Id,
            // A StopId that doesn't belong to this Trip's Route — must not be trusted blindly.
            Stops = [new StopDepartureTimeUpdate { StopId = Guid.NewGuid(), DepartureTime = DateTime.UtcNow }]
        };

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ValidStopIds_UpdatesDepartureTimesForWholeBatch()
    {
        var organizationId = Guid.NewGuid();
        var trip = CreateTrip(organizationId);
        var route = new Route { Id = Guid.NewGuid(), TripId = trip.Id, EstimatedDuration = TimeSpan.FromHours(8), DistanceKm = 700m };
        var originStop = new Stop { Id = Guid.NewGuid(), RouteId = route.Id, LocationId = Guid.NewGuid(), Order = 0, Type = Checkbus.ApiService.Domain.Enums.StopType.Origen };
        var destinationStop = new Stop { Id = Guid.NewGuid(), RouteId = route.Id, LocationId = Guid.NewGuid(), Order = 1, Type = Checkbus.ApiService.Domain.Enums.StopType.Destino };
        var tripRepository = new FakeTripRepository(trip, route, [originStop, destinationStop]);

        var handler = new UpdateStopTimesCommandHandler(tripRepository, new FakeCurrentUserService(organizationId));

        var newOriginTime = new DateTime(2026, 5, 1, 9, 0, 0, DateTimeKind.Unspecified);
        var newDestinationTime = new DateTime(2026, 5, 1, 17, 0, 0, DateTimeKind.Unspecified);
        var command = new UpdateStopTimesCommand
        {
            TripId = trip.Id,
            Stops =
            [
                new StopDepartureTimeUpdate { StopId = originStop.Id, DepartureTime = newOriginTime },
                new StopDepartureTimeUpdate { StopId = destinationStop.Id, DepartureTime = newDestinationTime }
            ]
        };

        await handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.NotNull(tripRepository.UpdatedDepartureTimesByStopId);
        Assert.Equal(2, tripRepository.UpdatedDepartureTimesByStopId!.Count);
        Assert.Equal(DateTime.SpecifyKind(newOriginTime, DateTimeKind.Utc), tripRepository.UpdatedDepartureTimesByStopId[originStop.Id]);
        Assert.Equal(DateTime.SpecifyKind(newDestinationTime, DateTimeKind.Utc), tripRepository.UpdatedDepartureTimesByStopId[destinationStop.Id]);
    }
}
