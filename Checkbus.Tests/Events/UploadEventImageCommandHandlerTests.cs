using Checkbus.ApiService.Application.Events.Commands;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Interfaces.Storage;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Trips;

namespace Checkbus.Tests.Events;

public class UploadEventImageCommandHandlerTests
{
    private sealed class FakeEventRepository(Event? @event) : IEventRepository
    {
        public List<Event> UpdatedEvents { get; } = [];

        public Task AddAsync(Event @event, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UploadEventImageCommandHandler.");

        public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(@event);

        public Task<IReadOnlyList<Event>> GetAllAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UploadEventImageCommandHandler.");

        public Task UpdateAsync(Event @event, CancellationToken cancellationToken)
        {
            UpdatedEvents.Add(@event);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeFileStorageService : IFileStorageService
    {
        public List<string> SavedKeys { get; } = [];
        public List<string> DeletedKeys { get; } = [];

        public Task<string> SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken)
        {
            SavedKeys.Add(key);
            return Task.FromResult(key);
        }

        public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UploadEventImageCommandHandler.");

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UploadEventImageCommandHandler.");

        public Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            DeletedKeys.Add(key);
            return Task.CompletedTask;
        }
    }

    private static Event CreateEvent(string? imageKey = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Final Copa Argentina",
        Type = EventType.Partido,
        Date = DateTime.UtcNow,
        LocationId = Guid.NewGuid(),
        ImageKey = imageKey,
        ImageContentType = imageKey is not null ? "image/jpeg" : null,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static UploadEventImageCommand CreateCommand(Guid eventId) => new()
    {
        EventId = eventId,
        FileStream = new MemoryStream([1, 2, 3]),
        ContentType = "image/jpeg",
        FileSizeBytes = 3,
        OriginalFileName = "banda.jpg"
    };

    [Fact]
    public async Task Handle_NoExistingImage_SavesNewImageAndUpdatesEvent()
    {
        var @event = CreateEvent();
        var eventRepository = new FakeEventRepository(@event);
        var fileStorage = new FakeFileStorageService();
        var handler = new UploadEventImageCommandHandler(eventRepository, fileStorage);

        await handler.Handle(CreateCommand(@event.Id), TestContext.Current.CancellationToken);

        Assert.Empty(fileStorage.DeletedKeys);
        Assert.Single(fileStorage.SavedKeys);
        var updated = Assert.Single(eventRepository.UpdatedEvents);
        Assert.Same(@event, updated);
        Assert.NotNull(updated.ImageKey);
        Assert.Equal("image/jpeg", updated.ImageContentType);
    }

    [Fact]
    public async Task Handle_ReUpload_DeletesOldImageKey()
    {
        var @event = CreateEvent(imageKey: "events/old-event-id/old-key.jpg");
        var eventRepository = new FakeEventRepository(@event);
        var fileStorage = new FakeFileStorageService();
        var handler = new UploadEventImageCommandHandler(eventRepository, fileStorage);

        await handler.Handle(CreateCommand(@event.Id), TestContext.Current.CancellationToken);

        Assert.Contains("events/old-event-id/old-key.jpg", fileStorage.DeletedKeys);
        Assert.Single(fileStorage.SavedKeys);
        var updated = Assert.Single(eventRepository.UpdatedEvents);
        Assert.NotEqual("events/old-event-id/old-key.jpg", updated.ImageKey);
    }

    [Fact]
    public async Task Handle_MissingEvent_ThrowsEventNotFoundException()
    {
        var eventRepository = new FakeEventRepository(@event: null);
        var fileStorage = new FakeFileStorageService();
        var handler = new UploadEventImageCommandHandler(eventRepository, fileStorage);

        await Assert.ThrowsAsync<EventNotFoundException>(
            () => handler.Handle(CreateCommand(Guid.NewGuid()), TestContext.Current.CancellationToken));

        Assert.Empty(fileStorage.SavedKeys);
        Assert.Empty(eventRepository.UpdatedEvents);
    }
}
