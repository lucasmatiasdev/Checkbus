using Checkbus.ApiService.Application.Events.Queries;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Trips;

namespace Checkbus.Tests.Events;

public class GetEventImageQueryHandlerTests
{
    private sealed class FakeEventRepository(Event? @event) : IEventRepository
    {
        public Task AddAsync(Event @event, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetEventImageQueryHandler.");

        public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(@event);

        public Task<IReadOnlyList<Event>> GetAllAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetEventImageQueryHandler.");

        public Task UpdateAsync(Event @event, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetEventImageQueryHandler.");
    }

    private static Event CreateEvent(string? imageKey, string? imageContentType) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Final Copa Argentina",
        Type = EventType.Partido,
        Date = DateTime.UtcNow,
        LocationId = Guid.NewGuid(),
        ImageKey = imageKey,
        ImageContentType = imageContentType,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task Handle_EventWithImage_ReturnsImageKeyAndContentType()
    {
        var @event = CreateEvent("events/some/key.jpg", "image/jpeg");
        var handler = new GetEventImageQueryHandler(new FakeEventRepository(@event));

        var result = await handler.Handle(
            new GetEventImageQuery { EventId = @event.Id }, TestContext.Current.CancellationToken);

        Assert.Equal(@event.ImageKey, result.ImageKey);
        Assert.Equal(@event.ImageContentType, result.ImageContentType);
    }

    [Fact]
    public async Task Handle_EventWithoutImage_ReturnsNullKey()
    {
        var @event = CreateEvent(imageKey: null, imageContentType: null);
        var handler = new GetEventImageQueryHandler(new FakeEventRepository(@event));

        var result = await handler.Handle(
            new GetEventImageQuery { EventId = @event.Id }, TestContext.Current.CancellationToken);

        Assert.Null(result.ImageKey);
        Assert.Null(result.ImageContentType);
    }

    [Fact]
    public async Task Handle_MissingEvent_ThrowsEventNotFoundException()
    {
        var handler = new GetEventImageQueryHandler(new FakeEventRepository(@event: null));

        await Assert.ThrowsAsync<EventNotFoundException>(() => handler.Handle(
            new GetEventImageQuery { EventId = Guid.NewGuid() }, TestContext.Current.CancellationToken));
    }
}
