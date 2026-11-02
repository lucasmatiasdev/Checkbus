using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Queries;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.Tests.Trips;

/// <summary>
/// Proves <see cref="GetTripLocationQueryHandler"/> (demo-only,
/// odd/tasks/seguimiento-tiempo-real-demo.md T2): same public, unauthenticated posture as
/// <see cref="GetPublicTripDetailQueryHandlerTests"/>, but collapses "doesn't exist", "not in
/// progress" and "tracking off" into the identical not-tracking shape instead of throwing.
/// </summary>
public class GetTripLocationQueryHandlerTests
{
    private sealed class FakeTripRepository(Trip? trip) : ITripRepository
    {
        public Task AddAsync(Trip t, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripLocationQueryHandler.");

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(trip);

        public Task UpdateAsync(Trip t, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripLocationQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripLocationQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(Guid vehicleId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripLocationQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(Guid driverId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripLocationQueryHandler.");

        public Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripLocationQueryHandler.");

        public Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripLocationQueryHandler.");

        public Task UpdateStopDepartureTimesAsync(
            IReadOnlyDictionary<Guid, DateTime> departureTimesByStopId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripLocationQueryHandler.");

        public Task<bool> SlugExistsAsync(Guid organizationId, string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripLocationQueryHandler.");

        public Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripLocationQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetPublicUpcomingByOrganizationIdAsync(
            Guid organizationId, DateTime? dateFrom, DateTime? dateTo, string? eventName, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripLocationQueryHandler.");
    }

    private static Trip CreateTrip(TripStatus status, bool liveTrackingEnabled, double? lat = null, double? lng = null, DateTime? updatedAt = null) => new()
    {
        Id = Guid.NewGuid(),
        Slug = Guid.NewGuid().ToString(),
        OrganizationId = Guid.NewGuid(),
        DriverId = Guid.NewGuid(),
        VehicleId = Guid.NewGuid(),
        EventId = Guid.NewGuid(),
        DepartureDate = DateTime.UtcNow,
        ArrivalDate = DateTime.UtcNow.AddHours(4),
        Capacity = 45,
        AvailableSeats = 45,
        Price = 15000m,
        Status = status,
        LiveTrackingEnabled = liveTrackingEnabled,
        CurrentLatitude = lat,
        CurrentLongitude = lng,
        LocationUpdatedAt = updatedAt
    };

    [Fact]
    public async Task Handle_TripInProcessAndTracking_ReturnsLastKnownPosition()
    {
        var updatedAt = DateTime.UtcNow;
        var trip = CreateTrip(TripStatus.EnProceso, liveTrackingEnabled: true, lat: -34.6037, lng: -58.3816, updatedAt: updatedAt);
        var handler = new GetTripLocationQueryHandler(new FakeTripRepository(trip));

        var result = await handler.Handle(new GetTripLocationQuery { TripId = trip.Id }, TestContext.Current.CancellationToken);

        Assert.True(result.LiveTrackingEnabled);
        Assert.Equal(-34.6037, result.Latitude);
        Assert.Equal(-58.3816, result.Longitude);
        Assert.Equal(updatedAt, result.UpdatedAt);
    }

    [Fact]
    public async Task Handle_UnknownTripId_ReturnsNotTrackingWithoutThrowing()
    {
        var handler = new GetTripLocationQueryHandler(new FakeTripRepository(null));

        var result = await handler.Handle(new GetTripLocationQuery { TripId = Guid.NewGuid() }, TestContext.Current.CancellationToken);

        Assert.False(result.LiveTrackingEnabled);
        Assert.Null(result.Latitude);
    }

    [Fact]
    public async Task Handle_TripNotInProcess_ReturnsNotTrackingEvenIfFlagWasLeftOn()
    {
        var trip = CreateTrip(TripStatus.Completado, liveTrackingEnabled: true, lat: 1, lng: 1, updatedAt: DateTime.UtcNow);
        var handler = new GetTripLocationQueryHandler(new FakeTripRepository(trip));

        var result = await handler.Handle(new GetTripLocationQuery { TripId = trip.Id }, TestContext.Current.CancellationToken);

        Assert.False(result.LiveTrackingEnabled);
        Assert.Null(result.Latitude);
    }

    [Fact]
    public async Task Handle_TrackingTurnedOff_ReturnsNotTracking()
    {
        var trip = CreateTrip(TripStatus.EnProceso, liveTrackingEnabled: false);
        var handler = new GetTripLocationQueryHandler(new FakeTripRepository(trip));

        var result = await handler.Handle(new GetTripLocationQuery { TripId = trip.Id }, TestContext.Current.CancellationToken);

        Assert.False(result.LiveTrackingEnabled);
    }
}
