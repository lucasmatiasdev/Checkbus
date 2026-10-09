using Checkbus.ApiService.Application.Events.Queries;
using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.Tests.Events;

public class GetEventsQueryHandlerTests
{
    private sealed class FakeEventRepository(IReadOnlyList<Event> events) : IEventRepository
    {
        public Guid? ReceivedOrganizationId { get; private set; }

        public Task AddAsync(Event @event, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetEventsQueryHandler.");

        public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetEventsQueryHandler.");

        public Task<IReadOnlyList<Event>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            ReceivedOrganizationId = organizationId;
            return Task.FromResult(events.Where(e => e.OrganizationId == organizationId).ToList() as IReadOnlyList<Event>);
        }

        public Task UpdateAsync(Event @event, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetEventsQueryHandler.");
    }

    private sealed class FakeLocationRepository(IReadOnlyList<Location> locations) : ILocationRepository
    {
        public int GetByIdCallCount { get; private set; }

        public Task<Location?> FindByPlaceIdAsync(string placeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetEventsQueryHandler.");

        public Task AddAsync(Location location, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetEventsQueryHandler.");

        public Task<Location?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            GetByIdCallCount++;
            return Task.FromResult(locations.FirstOrDefault(u => u.Id == id));
        }
    }

    private sealed class FakeCurrentUserService(Guid? organizationId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.NewGuid();
        public Guid? OrganizationId => organizationId;
        public string? Role => "Planificador";
        public string? Email => "planificador@checkbus-demo.com";
    }

    private static Location CreateLocation(string nombre) => new()
    {
        Id = Guid.NewGuid(),
        Name = nombre,
        Address = "Dirección de prueba",
        PlaceId = $"place-{Guid.NewGuid()}",
        Latitude = -31.0,
        Longitude = -64.0,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static Event CreateEvent(Guid organizationId, Guid locationId, string nombre) => new()
    {
        Id = Guid.NewGuid(),
        Name = nombre,
        Type = EventType.Concierto,
        Date = new DateTime(2026, 6, 1, 21, 0, 0, DateTimeKind.Utc),
        LocationId = locationId,
        OrganizationId = organizationId,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task Handle_ReturnsEventsWithResolvedLocation()
    {
        var organizationId = Guid.NewGuid();
        var location = CreateLocation("Movistar Arena");
        var @event = CreateEvent(organizationId, location.Id, "Recital de rock");
        var handler = new GetEventsQueryHandler(
            new FakeEventRepository([@event]), new FakeLocationRepository([location]), new FakeCurrentUserService(organizationId));

        var result = await handler.Handle(new GetEventsQuery(), TestContext.Current.CancellationToken);

        var dto = Assert.Single(result);
        Assert.Equal(@event.Id, dto.Id);
        Assert.Equal("Recital de rock", dto.Name);
        Assert.Equal("Movistar Arena", dto.Location.Name);
    }

    [Fact]
    public async Task Handle_MultipleEventsSharingLocation_ResolvesEachLocationOnlyOnce()
    {
        var organizationId = Guid.NewGuid();
        var location = CreateLocation("Estadio Monumental");
        var eventA = CreateEvent(organizationId, location.Id, "Partido A");
        var eventB = CreateEvent(organizationId, location.Id, "Partido B");
        var locationRepository = new FakeLocationRepository([location]);
        var handler = new GetEventsQueryHandler(
            new FakeEventRepository([eventA, eventB]), locationRepository, new FakeCurrentUserService(organizationId));

        var result = await handler.Handle(new GetEventsQuery(), TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.All(result, dto => Assert.Equal("Estadio Monumental", dto.Location.Name));
        Assert.Equal(1, locationRepository.GetByIdCallCount);
    }

    [Fact]
    public async Task Handle_NoEvents_ReturnsEmptyList()
    {
        var handler = new GetEventsQueryHandler(
            new FakeEventRepository([]), new FakeLocationRepository([]), new FakeCurrentUserService(Guid.NewGuid()));

        var result = await handler.Handle(new GetEventsQuery(), TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsRepositoryWithCallersOrganizationId()
    {
        var organizationId = Guid.NewGuid();
        var repository = new FakeEventRepository([]);
        var handler = new GetEventsQueryHandler(repository, new FakeLocationRepository([]), new FakeCurrentUserService(organizationId));

        await handler.Handle(new GetEventsQuery(), TestContext.Current.CancellationToken);

        Assert.Equal(organizationId, repository.ReceivedOrganizationId);
    }

    [Fact]
    public async Task Handle_EventFromOtherOrganization_IsExcluded()
    {
        var organizationId = Guid.NewGuid();
        var otherOrganizationId = Guid.NewGuid();
        var location = CreateLocation("Movistar Arena");
        var ownEvent = CreateEvent(organizationId, location.Id, "Propio");
        var otherEvent = CreateEvent(otherOrganizationId, location.Id, "Ajeno");
        var handler = new GetEventsQueryHandler(
            new FakeEventRepository([ownEvent, otherEvent]), new FakeLocationRepository([location]), new FakeCurrentUserService(organizationId));

        var result = await handler.Handle(new GetEventsQuery(), TestContext.Current.CancellationToken);

        var dto = Assert.Single(result);
        Assert.Equal(ownEvent.Id, dto.Id);
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var handler = new GetEventsQueryHandler(
            new FakeEventRepository([]), new FakeLocationRepository([]), new FakeCurrentUserService(organizationId: null));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new GetEventsQuery(), TestContext.Current.CancellationToken));
    }
}
