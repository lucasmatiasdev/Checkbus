using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Queries;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Trips;

namespace Checkbus.Tests.Trips;

/// <summary>
/// Proves <see cref="GetPublicTripEventImageQueryHandler"/> (odd/tasks/detalle-reserva-publica.md
/// T4) — resolves the Step 1 event image through the Trip, same public, unauthenticated posture
/// (Trip.Status == Publicado gate) as every other handler on this surface.
/// </summary>
public class GetPublicTripEventImageQueryHandlerTests
{
    private sealed class FakeTripRepository(Trip? trip) : ITripRepository
    {
        public Task AddAsync(Trip trip, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripEventImageQueryHandler.");

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(trip);

        public Task UpdateAsync(Trip trip, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripEventImageQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripEventImageQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(Guid vehicleId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripEventImageQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(Guid driverId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripEventImageQueryHandler.");

        public Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripEventImageQueryHandler.");

        public Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripEventImageQueryHandler.");
    }

    private sealed class FakeEventRepository(Event? @event) : IEventRepository
    {
        public Task AddAsync(Event @event, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripEventImageQueryHandler.");

        public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(@event);

        public Task UpdateAsync(Event @event, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripEventImageQueryHandler.");

        public Task<IReadOnlyList<Event>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripEventImageQueryHandler.");
    }

    private static Trip CreateTrip(Guid eventId, TripStatus status = TripStatus.Publicado) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = Guid.NewGuid(),
        DriverId = Guid.NewGuid(),
        VehicleId = Guid.NewGuid(),
        EventId = eventId,
        DepartureDate = DateTime.UtcNow,
        ArrivalDate = DateTime.UtcNow.AddHours(4),
        Capacity = 45,
        AvailableSeats = 45,
        Price = 15000m,
        Status = status
    };

    [Fact]
    public async Task Handle_EventWithImage_ReturnsImageKeyAndContentType()
    {
        var eventId = Guid.NewGuid();
        var trip = CreateTrip(eventId);
        var @event = new Event
        {
            Id = eventId,
            Name = "Coldplay en River",
            Type = EventType.Concierto,
            Date = trip.DepartureDate,
            LocationId = Guid.NewGuid(),
            ImageKey = "events/some-key.jpg",
            ImageContentType = "image/jpeg"
        };

        var handler = new GetPublicTripEventImageQueryHandler(new FakeTripRepository(trip), new FakeEventRepository(@event));

        var result = await handler.Handle(
            new GetPublicTripEventImageQuery { TripId = trip.Id }, TestContext.Current.CancellationToken);

        Assert.Equal("events/some-key.jpg", result.ImageKey);
        Assert.Equal("image/jpeg", result.ImageContentType);
    }

    [Fact]
    public async Task Handle_EventWithNoImage_ReturnsNullImageKey()
    {
        var eventId = Guid.NewGuid();
        var trip = CreateTrip(eventId);
        var @event = new Event
        {
            Id = eventId,
            Name = "River Plate vs Boca",
            Type = EventType.Partido,
            Date = trip.DepartureDate,
            LocationId = Guid.NewGuid()
        };

        var handler = new GetPublicTripEventImageQueryHandler(new FakeTripRepository(trip), new FakeEventRepository(@event));

        var result = await handler.Handle(
            new GetPublicTripEventImageQuery { TripId = trip.Id }, TestContext.Current.CancellationToken);

        Assert.Null(result.ImageKey);
    }

    [Fact]
    public async Task Handle_TripNotFound_ThrowsTripNotFound()
    {
        var handler = new GetPublicTripEventImageQueryHandler(new FakeTripRepository(null), new FakeEventRepository(null));

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(new GetPublicTripEventImageQuery { TripId = Guid.NewGuid() }, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(TripStatus.Programado)]
    [InlineData(TripStatus.EnProceso)]
    [InlineData(TripStatus.Completado)]
    [InlineData(TripStatus.Cancelado)]
    public async Task Handle_TripExistsButNotPublicado_ThrowsSameTripNotFound(TripStatus status)
    {
        var eventId = Guid.NewGuid();
        var trip = CreateTrip(eventId, status);
        var @event = new Event
        {
            Id = eventId,
            Name = "Coldplay en River",
            Type = EventType.Concierto,
            Date = trip.DepartureDate,
            LocationId = Guid.NewGuid(),
            ImageKey = "events/some-key.jpg"
        };

        var handler = new GetPublicTripEventImageQueryHandler(new FakeTripRepository(trip), new FakeEventRepository(@event));

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(new GetPublicTripEventImageQuery { TripId = trip.Id }, TestContext.Current.CancellationToken));
    }
}
