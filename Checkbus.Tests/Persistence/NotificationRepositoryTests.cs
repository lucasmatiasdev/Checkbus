using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Notifications;
using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Infrastructure.Implementations.Repositories;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Tests.Persistence;

/// <summary>
/// Proves <see cref="NotificationRepository"/> (CU-04, odd/tasks/notificaciones-viaje.md T1):
/// plain persistence round-trip, and GetRelevantToTripAsync's OR-of-two-conditions union
/// (direct Trip match OR via-Event match) — including that an unrelated Trip/Event never
/// leaks into the result.
/// </summary>
public class NotificationRepositoryTests
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

    private static Organization CreateOrganization(string slug) => new()
    {
        Id = Guid.NewGuid(),
        CUIT = "20-12345678-9",
        Name = "Checkbus Demo",
        Slug = slug,
        LogoUrl = "",
        IsActive = true
    };

    private static User CreateUser(Guid organizationId, string documentNumber) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Ana",
        Surname = "Lopez",
        Email = $"{documentNumber}@checkbus.test",
        PasswordHash = "hash",
        DocumentNumber = documentNumber,
        Role = Role.Administrador,
        OrganizationId = organizationId
    };

    private static Notification CreateNotification(
        Guid organizationId,
        NotificationTargetType targetType,
        Guid targetId,
        Guid createdByUserId,
        NotificationReason reason = NotificationReason.CambioHorario) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = organizationId,
        TargetType = targetType,
        TargetId = targetId,
        Reason = reason,
        Message = "El horario de salida cambió.",
        CreatedAt = DateTime.UtcNow,
        CreatedByUserId = createdByUserId
    };

    private async Task<(CheckbusDbContext Db, SqliteConnection Connection, Organization Organization, User User)> SeedAsync(
        CancellationToken cancellationToken)
    {
        var connection = CreateOpenConnection();
        var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization("checkbus-demo");
        var user = CreateUser(organization.Id, "30111222");
        db.Organizations.Add(organization);
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return (db, connection, organization, user);
    }

    [Fact]
    public async Task AddAsync_ThenReadBack_RoundTrips()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var (db, connection, organization, user) = await SeedAsync(cancellationToken);
        using var _ = connection;
        using var __ = db;

        var repository = new NotificationRepository(db);
        var tripId = Guid.NewGuid();
        var notification = CreateNotification(
            organization.Id, NotificationTargetType.Trip, tripId, user.Id, NotificationReason.Otro);

        await repository.AddAsync(notification, cancellationToken);

        var persisted = await db.Notifications.FirstOrDefaultAsync(n => n.Id == notification.Id, cancellationToken);
        Assert.NotNull(persisted);
        Assert.Equal(NotificationTargetType.Trip, persisted!.TargetType);
        Assert.Equal(tripId, persisted.TargetId);
        Assert.Equal(NotificationReason.Otro, persisted.Reason);
        Assert.Equal("El horario de salida cambió.", persisted.Message);
        Assert.Equal(user.Id, persisted.CreatedByUserId);
        Assert.Equal(organization.Id, persisted.OrganizationId);
    }

    [Fact]
    public async Task GetRelevantToTripAsync_ReturnsDirectTripMatch()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var (db, connection, organization, user) = await SeedAsync(cancellationToken);
        using var _ = connection;
        using var __ = db;

        var repository = new NotificationRepository(db);
        var tripId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var directNotification = CreateNotification(organization.Id, NotificationTargetType.Trip, tripId, user.Id);
        await repository.AddAsync(directNotification, cancellationToken);

        var result = await repository.GetRelevantToTripAsync(tripId, eventId, cancellationToken);

        Assert.Single(result);
        Assert.Equal(directNotification.Id, result[0].Id);
    }

    [Fact]
    public async Task GetRelevantToTripAsync_ReturnsViaEventMatch()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var (db, connection, organization, user) = await SeedAsync(cancellationToken);
        using var _ = connection;
        using var __ = db;

        var repository = new NotificationRepository(db);
        var tripId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var eventScopedNotification = CreateNotification(organization.Id, NotificationTargetType.Event, eventId, user.Id);
        await repository.AddAsync(eventScopedNotification, cancellationToken);

        var result = await repository.GetRelevantToTripAsync(tripId, eventId, cancellationToken);

        Assert.Single(result);
        Assert.Equal(eventScopedNotification.Id, result[0].Id);
    }

    [Fact]
    public async Task GetRelevantToTripAsync_UnionsBothDirectAndViaEvent_ExcludingUnrelatedRows()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var (db, connection, organization, user) = await SeedAsync(cancellationToken);
        using var _ = connection;
        using var __ = db;

        var repository = new NotificationRepository(db);
        var tripId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var unrelatedTripId = Guid.NewGuid();
        var unrelatedEventId = Guid.NewGuid();

        var directNotification = CreateNotification(organization.Id, NotificationTargetType.Trip, tripId, user.Id);
        var eventScopedNotification = CreateNotification(organization.Id, NotificationTargetType.Event, eventId, user.Id);
        var unrelatedTripNotification = CreateNotification(organization.Id, NotificationTargetType.Trip, unrelatedTripId, user.Id);
        var unrelatedEventNotification = CreateNotification(organization.Id, NotificationTargetType.Event, unrelatedEventId, user.Id);

        await repository.AddAsync(directNotification, cancellationToken);
        await repository.AddAsync(eventScopedNotification, cancellationToken);
        await repository.AddAsync(unrelatedTripNotification, cancellationToken);
        await repository.AddAsync(unrelatedEventNotification, cancellationToken);

        var result = await repository.GetRelevantToTripAsync(tripId, eventId, cancellationToken);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, n => n.Id == directNotification.Id);
        Assert.Contains(result, n => n.Id == eventScopedNotification.Id);
        Assert.DoesNotContain(result, n => n.Id == unrelatedTripNotification.Id);
        Assert.DoesNotContain(result, n => n.Id == unrelatedEventNotification.Id);
    }
}
