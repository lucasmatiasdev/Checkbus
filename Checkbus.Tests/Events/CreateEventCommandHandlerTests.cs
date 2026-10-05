using Checkbus.ApiService.Application.Events.Commands;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.Tests.Events;

public class CreateEventCommandHandlerTests
{
    private sealed class FakeLocationRepository(Location? existingByPlaceId) : ILocationRepository
    {
        public List<Location> AddedLocations { get; } = [];

        public Task<Location?> FindByPlaceIdAsync(string placeId, CancellationToken cancellationToken)
            => Task.FromResult(existingByPlaceId is not null && existingByPlaceId.PlaceId == placeId
                ? existingByPlaceId
                : null);

        public Task AddAsync(Location location, CancellationToken cancellationToken)
        {
            AddedLocations.Add(location);
            return Task.CompletedTask;
        }

        public Task<Location?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateEventCommandHandler.");
    }

    private sealed class FakeEventRepository : IEventRepository
    {
        public List<Event> AddedEvents { get; } = [];

        public Task AddAsync(Event @event, CancellationToken cancellationToken)
        {
            AddedEvents.Add(@event);
            return Task.CompletedTask;
        }

        public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateEventCommandHandler.");

        public Task<IReadOnlyList<Event>> GetAllAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateEventCommandHandler.");
    }

    private static CreateEventCommand CreateCommand() => new()
    {
        Name = "Final Copa Argentina",
        Type = EventType.Partido,
        Date = new DateTime(2026, 5, 1, 20, 0, 0, DateTimeKind.Utc),
        LocationName = "Estadio Mario Alberto Kempes",
        Address = "Av. Cardeñosa, Córdoba",
        PlaceId = "place-kempes-1",
        Latitude = -31.3333,
        Longitude = -64.2333
    };

    [Fact]
    public async Task Handle_NewPlaceId_CreatesLocationAndEvent()
    {
        var locationRepository = new FakeLocationRepository(existingByPlaceId: null);
        var eventRepository = new FakeEventRepository();
        var handler = new CreateEventCommandHandler(locationRepository, eventRepository);

        var result = await handler.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        var createdLocation = Assert.Single(locationRepository.AddedLocations);
        Assert.Equal("place-kempes-1", createdLocation.PlaceId);
        Assert.Equal("Estadio Mario Alberto Kempes", createdLocation.Name);
        Assert.Equal("Av. Cardeñosa, Córdoba", createdLocation.Address);
        Assert.Equal(-31.3333, createdLocation.Latitude);
        Assert.Equal(-64.2333, createdLocation.Longitude);

        var createdEvent = Assert.Single(eventRepository.AddedEvents);
        Assert.Equal(createdLocation.Id, createdEvent.LocationId);
        Assert.Equal("Final Copa Argentina", createdEvent.Name);
        Assert.Equal(EventType.Partido, createdEvent.Type);

        Assert.Equal(createdEvent.Id, result.Id);
        Assert.Equal("Final Copa Argentina", result.Name);
        Assert.Equal(EventType.Partido, result.Type);
        Assert.Equal("Estadio Mario Alberto Kempes", result.Location.Name);
        Assert.Equal("Av. Cardeñosa, Córdoba", result.Location.Address);
    }

    [Fact]
    public async Task Handle_ExistingPlaceId_ReusesLocation_DoesNotCreateNewRow()
    {
        var existing = new Location
        {
            Id = Guid.NewGuid(),
            Name = "Estadio Mario Alberto Kempes",
            Address = "Av. Cardeñosa, Córdoba",
            PlaceId = "place-kempes-1",
            Latitude = -31.3333,
            Longitude = -64.2333,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var locationRepository = new FakeLocationRepository(existing);
        var eventRepository = new FakeEventRepository();
        var handler = new CreateEventCommandHandler(locationRepository, eventRepository);

        var result = await handler.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        Assert.Empty(locationRepository.AddedLocations);

        var createdEvent = Assert.Single(eventRepository.AddedEvents);
        Assert.Equal(existing.Id, createdEvent.LocationId);
        Assert.Equal(existing.Name, result.Location.Name);
    }

    [Fact]
    public async Task Handle_ReturnsDtoMappedFromCreatedEventAndLocation()
    {
        var locationRepository = new FakeLocationRepository(existingByPlaceId: null);
        var eventRepository = new FakeEventRepository();
        var handler = new CreateEventCommandHandler(locationRepository, eventRepository);
        var command = CreateCommand();

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.Equal(command.Name, result.Name);
        Assert.Equal(command.Type, result.Type);
        Assert.Equal(command.Date, result.Date);
        Assert.Equal(command.LocationName, result.Location.Name);
        Assert.Equal(command.Address, result.Location.Address);
        Assert.Equal(command.Latitude, result.Location.Latitude);
        Assert.Equal(command.Longitude, result.Location.Longitude);
    }
}
