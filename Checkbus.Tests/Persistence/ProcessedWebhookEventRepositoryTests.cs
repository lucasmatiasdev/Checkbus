using Checkbus.ApiService.Infrastructure.Implementations.Payments;
using Checkbus.ApiService.Infrastructure.Implementations.Repositories;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Tests.Persistence;

/// <summary>
/// Proves <see cref="ProcessedWebhookEventRepository"/> and the EF mapping/unique index for
/// <see cref="ProcessedWebhookEvent"/> (odd/tasks/pagos-mercadopago.md Fase D3) against the same
/// SQLite-in-memory setup every other persistence test in this folder uses.
/// </summary>
public class ProcessedWebhookEventRepositoryTests
{
    private static SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=True");
        connection.Open();
        return connection;
    }

    private static CheckbusDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<CheckbusDbContext>()
            .UseSqlite(connection)
            .Options;
        return new CheckbusDbContext(options);
    }

    [Fact]
    public async Task ExistsAsync_UnknownEventId_ReturnsFalse()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new ProcessedWebhookEventRepository(db);

        Assert.False(await repository.ExistsAsync("unknown-event", cancellationToken));
    }

    [Fact]
    public async Task RecordAsync_ThenExistsAsync_RoundTrips()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new ProcessedWebhookEventRepository(db);

        await repository.RecordAsync("event-123", cancellationToken);

        Assert.True(await repository.ExistsAsync("event-123", cancellationToken));
        Assert.False(await repository.ExistsAsync("event-456", cancellationToken));
    }

    [Fact]
    public async Task RecordAsync_SameEventIdTwice_ViolatesUniqueIndex()
    {
        // The whole point of the table (Fase D3's dedupe guarantee): the DB itself, not just
        // the handler's own check, must reject a second row for the same notification id.
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        db.ProcessedWebhookEvents.Add(new ProcessedWebhookEvent
        {
            Id = Guid.NewGuid(),
            ExternalEventId = "dup-event",
            ProcessedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);

        db.ProcessedWebhookEvents.Add(new ProcessedWebhookEvent
        {
            Id = Guid.NewGuid(),
            ExternalEventId = "dup-event",
            ProcessedAt = DateTime.UtcNow
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(cancellationToken));
    }
}
