using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Commands;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Trips;

namespace Checkbus.Tests.Trips;

/// <summary>
/// Proves <see cref="UpdateTripLocationCommandHandler"/> (demo-only,
/// odd/tasks/seguimiento-tiempo-real-demo.md T1): same IDOR-safe driver-owns-trip rule as
/// <see cref="ToggleTripLiveTrackingCommandHandlerTests"/>, the EnProceso + LiveTrackingEnabled
/// double gate, and that a successful ping overwrites the previous point (no history kept).
/// </summary>
public class UpdateTripLocationCommandHandlerTests
{
    private sealed class FakeTripRepository(Trip? trip) : ITripRepository
    {
        public Trip? UpdatedTrip { get; private set; }

        public Task AddAsync(Trip t, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateTripLocationCommandHandler.");

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(trip);

        public Task UpdateAsync(Trip t, CancellationToken cancellationToken)
        {
            UpdatedTrip = t;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateTripLocationCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(Guid vehicleId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateTripLocationCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(Guid driverId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateTripLocationCommandHandler.");

        public Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateTripLocationCommandHandler.");

        public Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateTripLocationCommandHandler.");

        public Task UpdateStopDepartureTimesAsync(
            IReadOnlyDictionary<Guid, DateTime> departureTimesByStopId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateTripLocationCommandHandler.");

        public Task<bool> SlugExistsAsync(Guid organizationId, string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateTripLocationCommandHandler.");

        public Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateTripLocationCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetPublicUpcomingByOrganizationIdAsync(
            Guid organizationId, DateTime? dateFrom, DateTime? dateTo, string? eventName, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateTripLocationCommandHandler.");
    }

    private sealed class FakeCurrentUserService(Guid? organizationId, Guid? userId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => userId;
        public Guid? OrganizationId => organizationId;
        public string? Role => "Chofer";
        public string? Email => "chofer@checkbus-demo.com";
    }

    private static Trip CreateTrip(Guid organizationId, Guid driverId, TripStatus status, bool liveTrackingEnabled) => new()
    {
        Id = Guid.NewGuid(),
        Slug = Guid.NewGuid().ToString(),
        OrganizationId = organizationId,
        DriverId = driverId,
        VehicleId = Guid.NewGuid(),
        EventId = Guid.NewGuid(),
        DepartureDate = DateTime.UtcNow,
        ArrivalDate = DateTime.UtcNow.AddHours(4),
        Capacity = 45,
        AvailableSeats = 45,
        Price = 15000m,
        Status = status,
        LiveTrackingEnabled = liveTrackingEnabled
    };

    [Fact]
    public async Task Handle_TrackingEnabledAndInProcess_UpdatesPositionAndTimestamp()
    {
        var organizationId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var trip = CreateTrip(organizationId, driverId, TripStatus.EnProceso, liveTrackingEnabled: true);
        var tripRepository = new FakeTripRepository(trip);
        var handler = new UpdateTripLocationCommandHandler(tripRepository, new FakeCurrentUserService(organizationId, driverId));

        await handler.Handle(
            new UpdateTripLocationCommand { TripId = trip.Id, Latitude = -34.6037, Longitude = -58.3816 },
            TestContext.Current.CancellationToken);

        Assert.Equal(-34.6037, tripRepository.UpdatedTrip!.CurrentLatitude);
        Assert.Equal(-58.3816, tripRepository.UpdatedTrip.CurrentLongitude);
        Assert.NotNull(tripRepository.UpdatedTrip.LocationUpdatedAt);
    }

    [Fact]
    public async Task Handle_TrackingNotEnabled_ThrowsTripNotEligibleException()
    {
        var organizationId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var trip = CreateTrip(organizationId, driverId, TripStatus.EnProceso, liveTrackingEnabled: false);
        var handler = new UpdateTripLocationCommandHandler(
            new FakeTripRepository(trip), new FakeCurrentUserService(organizationId, driverId));

        await Assert.ThrowsAsync<TripNotEligibleException>(() => handler.Handle(
            new UpdateTripLocationCommand { TripId = trip.Id, Latitude = 0, Longitude = 0 },
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_TripNotInProcess_ThrowsTripNotEligibleException()
    {
        var organizationId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var trip = CreateTrip(organizationId, driverId, TripStatus.Completado, liveTrackingEnabled: true);
        var handler = new UpdateTripLocationCommandHandler(
            new FakeTripRepository(trip), new FakeCurrentUserService(organizationId, driverId));

        await Assert.ThrowsAsync<TripNotEligibleException>(() => handler.Handle(
            new UpdateTripLocationCommand { TripId = trip.Id, Latitude = 0, Longitude = 0 },
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_CallerIsNotTripsDriver_ThrowsTripNotFoundException()
    {
        var organizationId = Guid.NewGuid();
        var trip = CreateTrip(organizationId, Guid.NewGuid(), TripStatus.EnProceso, liveTrackingEnabled: true);
        var handler = new UpdateTripLocationCommandHandler(
            new FakeTripRepository(trip), new FakeCurrentUserService(organizationId, Guid.NewGuid()));

        await Assert.ThrowsAsync<TripNotFoundException>(() => handler.Handle(
            new UpdateTripLocationCommand { TripId = trip.Id, Latitude = 0, Longitude = 0 },
            TestContext.Current.CancellationToken));
    }
}
