using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Commands;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Trips;

namespace Checkbus.Tests.Trips;

/// <summary>
/// Proves <see cref="ToggleTripLiveTrackingCommandHandler"/> (demo-only,
/// odd/tasks/seguimiento-tiempo-real-demo.md T1): same IDOR-safe driver-owns-trip rule as
/// <see cref="Checkbus.Tests.Boarding.ValidateBoardingCommandHandlerTests"/>, the EnProceso-only
/// gate, and that turning tracking off clears any stale point.
/// </summary>
public class ToggleTripLiveTrackingCommandHandlerTests
{
    private sealed class FakeTripRepository(Trip? trip) : ITripRepository
    {
        public Trip? UpdatedTrip { get; private set; }

        public Task AddAsync(Trip t, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ToggleTripLiveTrackingCommandHandler.");

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(trip);

        public Task UpdateAsync(Trip t, CancellationToken cancellationToken)
        {
            UpdatedTrip = t;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ToggleTripLiveTrackingCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(Guid vehicleId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ToggleTripLiveTrackingCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(Guid driverId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ToggleTripLiveTrackingCommandHandler.");

        public Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ToggleTripLiveTrackingCommandHandler.");

        public Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ToggleTripLiveTrackingCommandHandler.");

        public Task UpdateStopDepartureTimesAsync(
            IReadOnlyDictionary<Guid, DateTime> departureTimesByStopId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ToggleTripLiveTrackingCommandHandler.");

        public Task<bool> SlugExistsAsync(Guid organizationId, string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ToggleTripLiveTrackingCommandHandler.");

        public Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ToggleTripLiveTrackingCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetPublicUpcomingByOrganizationIdAsync(
            Guid organizationId, DateTime? dateFrom, DateTime? dateTo, string? eventName, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ToggleTripLiveTrackingCommandHandler.");
    }

    private sealed class FakeCurrentUserService(Guid? organizationId, Guid? userId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => userId;
        public Guid? OrganizationId => organizationId;
        public string? Role => "Chofer";
        public string? Email => "chofer@checkbus-demo.com";
    }

    private static Trip CreateTrip(Guid organizationId, Guid driverId, TripStatus status) => new()
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
        Status = status
    };

    [Fact]
    public async Task Handle_TripInProcess_EnablesTracking()
    {
        var organizationId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var trip = CreateTrip(organizationId, driverId, TripStatus.EnProceso);
        var tripRepository = new FakeTripRepository(trip);
        var handler = new ToggleTripLiveTrackingCommandHandler(tripRepository, new FakeCurrentUserService(organizationId, driverId));

        await handler.Handle(
            new ToggleTripLiveTrackingCommand { TripId = trip.Id, Enabled = true },
            TestContext.Current.CancellationToken);

        Assert.True(tripRepository.UpdatedTrip!.LiveTrackingEnabled);
    }

    [Fact]
    public async Task Handle_DisablingTracking_ClearsStalePosition()
    {
        var organizationId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var trip = CreateTrip(organizationId, driverId, TripStatus.EnProceso);
        trip.LiveTrackingEnabled = true;
        trip.CurrentLatitude = -34.6;
        trip.CurrentLongitude = -58.4;
        trip.LocationUpdatedAt = DateTime.UtcNow;
        var tripRepository = new FakeTripRepository(trip);
        var handler = new ToggleTripLiveTrackingCommandHandler(tripRepository, new FakeCurrentUserService(organizationId, driverId));

        await handler.Handle(
            new ToggleTripLiveTrackingCommand { TripId = trip.Id, Enabled = false },
            TestContext.Current.CancellationToken);

        Assert.False(tripRepository.UpdatedTrip!.LiveTrackingEnabled);
        Assert.Null(tripRepository.UpdatedTrip.CurrentLatitude);
        Assert.Null(tripRepository.UpdatedTrip.CurrentLongitude);
        Assert.Null(tripRepository.UpdatedTrip.LocationUpdatedAt);
    }

    [Fact]
    public async Task Handle_TripNotInProcess_ThrowsTripNotEligibleException()
    {
        var organizationId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var trip = CreateTrip(organizationId, driverId, TripStatus.Publicado);
        var handler = new ToggleTripLiveTrackingCommandHandler(
            new FakeTripRepository(trip), new FakeCurrentUserService(organizationId, driverId));

        await Assert.ThrowsAsync<TripNotEligibleException>(() => handler.Handle(
            new ToggleTripLiveTrackingCommand { TripId = trip.Id, Enabled = true },
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_CallerIsNotTripsDriver_ThrowsTripNotFoundException()
    {
        var organizationId = Guid.NewGuid();
        var trip = CreateTrip(organizationId, Guid.NewGuid(), TripStatus.EnProceso);
        var handler = new ToggleTripLiveTrackingCommandHandler(
            new FakeTripRepository(trip), new FakeCurrentUserService(organizationId, Guid.NewGuid()));

        await Assert.ThrowsAsync<TripNotFoundException>(() => handler.Handle(
            new ToggleTripLiveTrackingCommand { TripId = trip.Id, Enabled = true },
            TestContext.Current.CancellationToken));
    }
}
