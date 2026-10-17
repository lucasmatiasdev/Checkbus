using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Queries;
using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.Tests.Trips;

/// <summary>
/// Proves <see cref="GetPublicOrganizationTripsQueryHandler"/>
/// (odd/tasks/viajes-disponibles-publico.md T1) — the project's first public query keyed by
/// Organization.Slug. Fake-repository style, matching
/// <see cref="GetPublicTripSeatMapQueryHandlerTests"/>/<see cref="GetPublicTripDetailQueryHandlerTests"/>.
/// </summary>
public class GetPublicOrganizationTripsQueryHandlerTests
{
    private sealed class FakeOrganizationRepository(Guid? organizationId) : IOrganizationRepository
    {
        public string? ReceivedSlug { get; private set; }

        public Task AddAsync(Organization organization, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicOrganizationTripsQueryHandler.");

        public Task<string?> GetSlugByIdAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicOrganizationTripsQueryHandler.");

        public Task<string?> GetNameByIdAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicOrganizationTripsQueryHandler.");

        public Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            ReceivedSlug = slug;
            return Task.FromResult(organizationId);
        }

        public Task<bool> CuitExistsAsync(string cuit, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicOrganizationTripsQueryHandler.");

        public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicOrganizationTripsQueryHandler.");
    }

    private sealed class FakeTripRepository(IReadOnlyList<Trip> trips) : ITripRepository
    {
        public Task AddAsync(Trip trip, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicOrganizationTripsQueryHandler.");

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicOrganizationTripsQueryHandler.");

        public Task UpdateAsync(Trip trip, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicOrganizationTripsQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicOrganizationTripsQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(Guid vehicleId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicOrganizationTripsQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(Guid driverId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicOrganizationTripsQueryHandler.");

        public Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicOrganizationTripsQueryHandler.");

        public Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicOrganizationTripsQueryHandler.");

        public Task UpdateStopDepartureTimesAsync(
            IReadOnlyDictionary<Guid, DateTime> departureTimesByStopId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicOrganizationTripsQueryHandler.");

        public Task<bool> SlugExistsAsync(Guid organizationId, string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicOrganizationTripsQueryHandler.");

        public Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicOrganizationTripsQueryHandler.");

        public Guid? ReceivedOrganizationId { get; private set; }
        public DateTime? ReceivedDateFrom { get; private set; }
        public DateTime? ReceivedDateTo { get; private set; }
        public string? ReceivedEventName { get; private set; }

        public Task<IReadOnlyList<Trip>> GetPublicUpcomingByOrganizationIdAsync(
            Guid organizationId, DateTime? dateFrom, DateTime? dateTo, string? eventName, CancellationToken cancellationToken)
        {
            ReceivedOrganizationId = organizationId;
            ReceivedDateFrom = dateFrom;
            ReceivedDateTo = dateTo;
            ReceivedEventName = eventName;
            return Task.FromResult(trips);
        }
    }

    private sealed class FakeEventRepository(IReadOnlyList<Event> events) : IEventRepository
    {
        public Task AddAsync(Event @event, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicOrganizationTripsQueryHandler.");

        public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicOrganizationTripsQueryHandler.");

        public Task UpdateAsync(Event @event, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicOrganizationTripsQueryHandler.");

        public Task<IReadOnlyList<Event>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => Task.FromResult(events);
    }

    private static Trip CreateTrip(Guid organizationId, Guid eventId) => new()
    {
        Id = Guid.NewGuid(),
        Slug = "river-vs-boca-20261101",
        OrganizationId = organizationId,
        DriverId = Guid.NewGuid(),
        VehicleId = Guid.NewGuid(),
        EventId = eventId,
        DepartureDate = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc),
        ArrivalDate = new DateTime(2026, 11, 1, 18, 0, 0, DateTimeKind.Utc),
        Capacity = 45,
        AvailableSeats = 45,
        Price = 15000m,
        Status = TripStatus.Publicado
    };

    [Fact]
    public async Task Handle_UnknownOrganizationSlug_ReturnsEmptyListWithoutCallingTripRepository()
    {
        // Same IDOR-safe posture as GetPublicTripSeatMapQueryHandler, expressed as an empty list
        // rather than a 404 — see the handler's own doc comment.
        var organizationRepository = new FakeOrganizationRepository(null);
        var tripRepository = new FakeTripRepository([]);
        var handler = new GetPublicOrganizationTripsQueryHandler(
            organizationRepository, tripRepository, new FakeEventRepository([]));

        var result = await handler.Handle(
            new GetPublicOrganizationTripsQuery { OrganizationSlug = "does-not-exist" },
            TestContext.Current.CancellationToken);

        Assert.Empty(result);
        Assert.Null(tripRepository.ReceivedOrganizationId);
        Assert.Equal("does-not-exist", organizationRepository.ReceivedSlug);
    }

    [Fact]
    public async Task Handle_KnownSlugWithNoMatchingTrips_ReturnsEmptyList()
    {
        var organizationId = Guid.NewGuid();
        var handler = new GetPublicOrganizationTripsQueryHandler(
            new FakeOrganizationRepository(organizationId),
            new FakeTripRepository([]),
            new FakeEventRepository([]));

        var result = await handler.Handle(
            new GetPublicOrganizationTripsQuery { OrganizationSlug = "checkbus-demo" },
            TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_KnownSlugWithMatchingTrips_MapsThinDtoWithEventNameFromBatchLookup()
    {
        var organizationId = Guid.NewGuid();
        var @event = new Event
        {
            Id = Guid.NewGuid(),
            Name = "River vs Boca",
            Type = EventType.Partido,
            Date = new DateTime(2026, 11, 1, 20, 0, 0, DateTimeKind.Utc),
            LocationId = Guid.NewGuid(),
            OrganizationId = organizationId
        };
        var trip = CreateTrip(organizationId, @event.Id);

        var handler = new GetPublicOrganizationTripsQueryHandler(
            new FakeOrganizationRepository(organizationId),
            new FakeTripRepository([trip]),
            new FakeEventRepository([@event]));

        var result = await handler.Handle(
            new GetPublicOrganizationTripsQuery { OrganizationSlug = "checkbus-demo" },
            TestContext.Current.CancellationToken);

        var dto = Assert.Single(result);
        Assert.Equal(trip.Slug, dto.TripSlug);
        Assert.Equal("River vs Boca", dto.EventName);
        Assert.Equal(trip.DepartureDate, dto.DepartureDate);
        Assert.Equal(trip.Price, dto.Price);
    }

    [Fact]
    public async Task Handle_PassesDateAndEventNameFiltersThroughToRepository()
    {
        var organizationId = Guid.NewGuid();
        var tripRepository = new FakeTripRepository([]);
        var handler = new GetPublicOrganizationTripsQueryHandler(
            new FakeOrganizationRepository(organizationId),
            tripRepository,
            new FakeEventRepository([]));
        var dateFrom = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var dateTo = new DateTime(2026, 11, 30, 0, 0, 0, DateTimeKind.Utc);

        await handler.Handle(
            new GetPublicOrganizationTripsQuery
            {
                OrganizationSlug = "checkbus-demo",
                DateFrom = dateFrom,
                DateTo = dateTo,
                EventName = "river"
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(organizationId, tripRepository.ReceivedOrganizationId);
        Assert.Equal(dateFrom, tripRepository.ReceivedDateFrom);
        Assert.Equal(dateTo, tripRepository.ReceivedDateTo);
        Assert.Equal("river", tripRepository.ReceivedEventName);
    }
}
