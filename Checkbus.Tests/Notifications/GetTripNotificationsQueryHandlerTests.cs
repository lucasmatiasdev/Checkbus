using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Notifications.Queries;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Notifications;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Trips;

namespace Checkbus.Tests.Notifications;

/// <summary>
/// Proves <see cref="GetTripNotificationsQueryHandler"/> (CU-04): tenant-scoped IDOR-safe load
/// (mirrors every other Trip-scoped query handler in this module), delegating the actual
/// direct-plus-via-Event union to INotificationRepository.GetRelevantToTripAsync, and resolving
/// CreatedByUserId to a display name.
/// </summary>
public class GetTripNotificationsQueryHandlerTests
{
    private sealed class FakeCurrentUserService(Guid? organizationId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.NewGuid();
        public Guid? OrganizationId => organizationId;
        public string? Role => "Administrador";
        public string? Email => "admin@checkbus-demo.com";
    }

    private sealed class FakeTripRepository(Trip? trip) : ITripRepository
    {
        public Task AddAsync(Trip t, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripNotificationsQueryHandler.");

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(trip);

        public Task UpdateAsync(Trip t, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripNotificationsQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripNotificationsQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(Guid vehicleId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripNotificationsQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(Guid driverId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripNotificationsQueryHandler.");

        public Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripNotificationsQueryHandler.");

        public Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripNotificationsQueryHandler.");

        public Task UpdateStopDepartureTimesAsync(
            IReadOnlyDictionary<Guid, DateTime> departureTimesByStopId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripNotificationsQueryHandler.");

        public Task<bool> SlugExistsAsync(Guid organizationId, string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripNotificationsQueryHandler.");

        public Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripNotificationsQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetPublicUpcomingByOrganizationIdAsync(
            Guid organizationId, DateTime? dateFrom, DateTime? dateTo, string? eventName, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripNotificationsQueryHandler.");
    }

    private sealed class FakeNotificationRepository(IReadOnlyList<Notification> relevantNotifications) : INotificationRepository
    {
        public Guid? ReceivedTripId { get; private set; }
        public Guid? ReceivedEventId { get; private set; }

        public Task AddAsync(Notification notification, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripNotificationsQueryHandler.");

        public Task<IReadOnlyList<Notification>> GetRelevantToTripAsync(Guid tripId, Guid eventId, CancellationToken cancellationToken)
        {
            ReceivedTripId = tripId;
            ReceivedEventId = eventId;
            return Task.FromResult(relevantNotifications);
        }
    }

    private sealed class FakeUserRepository(IReadOnlyDictionary<Guid, User> usersById) : IUserRepository
    {
        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripNotificationsQueryHandler.");

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(usersById.TryGetValue(id, out var user) ? user : null);

        public Task AddAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripNotificationsQueryHandler.");

        public Task UpdateAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripNotificationsQueryHandler.");

        public Task<bool> DocumentNumberExistsInOrganizationAsync(string documentNumber, Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripNotificationsQueryHandler.");

        public Task<IReadOnlyList<string>> FindEmailsByLocalPartPrefixAsync(string localPartPrefix, string domain, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripNotificationsQueryHandler.");

        public Task<IReadOnlyList<User>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripNotificationsQueryHandler.");
    }

    private static Trip CreateTrip(Guid organizationId, Guid eventId) => new()
    {
        Id = Guid.NewGuid(),
        Slug = Guid.NewGuid().ToString(),
        OrganizationId = organizationId,
        DriverId = Guid.NewGuid(),
        VehicleId = Guid.NewGuid(),
        EventId = eventId,
        DepartureDate = DateTime.UtcNow.AddDays(5),
        ArrivalDate = DateTime.UtcNow.AddDays(5).AddHours(4),
        Capacity = 45,
        AvailableSeats = 45,
        Price = 15000m,
        Status = TripStatus.Publicado
    };

    private static Notification CreateNotification(Guid organizationId, Guid createdByUserId) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = organizationId,
        TargetType = NotificationTargetType.Trip,
        TargetId = Guid.NewGuid(),
        Reason = NotificationReason.CambioHorario,
        Message = "El horario cambió.",
        CreatedAt = DateTime.UtcNow,
        CreatedByUserId = createdByUserId
    };

    private static User CreateUser(Guid organizationId) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Ana",
        Surname = "Lopez",
        Email = "ana@checkbus.test",
        PasswordHash = "hash",
        DocumentNumber = "30111222",
        Role = Checkbus.ApiService.Domain.Authorization.Role.Administrador,
        OrganizationId = organizationId
    };

    [Fact]
    public async Task Handle_TripBelongsToCallerOrganization_ReturnsResolvedDtosWithCreatedByName()
    {
        var organizationId = Guid.NewGuid();
        var trip = CreateTrip(organizationId, Guid.NewGuid());
        var author = CreateUser(organizationId);
        var notification = CreateNotification(organizationId, author.Id);

        var notificationRepository = new FakeNotificationRepository([notification]);
        var handler = new GetTripNotificationsQueryHandler(
            new FakeTripRepository(trip),
            notificationRepository,
            new FakeUserRepository(new Dictionary<Guid, User> { [author.Id] = author }),
            new FakeCurrentUserService(organizationId));

        var result = await handler.Handle(
            new GetTripNotificationsQuery { TripId = trip.Id }, TestContext.Current.CancellationToken);

        Assert.Single(result);
        Assert.Equal(notification.Id, result[0].Id);
        Assert.Equal("Ana Lopez", result[0].CreatedByName);
        Assert.Equal(trip.Id, notificationRepository.ReceivedTripId);
        Assert.Equal(trip.EventId, notificationRepository.ReceivedEventId);
    }

    [Fact]
    public async Task Handle_TripFromAnotherOrganization_ThrowsTripNotFoundException()
    {
        var trip = CreateTrip(Guid.NewGuid(), Guid.NewGuid());
        var handler = new GetTripNotificationsQueryHandler(
            new FakeTripRepository(trip),
            new FakeNotificationRepository([]),
            new FakeUserRepository(new Dictionary<Guid, User>()),
            new FakeCurrentUserService(Guid.NewGuid()));

        await Assert.ThrowsAsync<TripNotFoundException>(() => handler.Handle(
            new GetTripNotificationsQuery { TripId = trip.Id }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_TripDoesNotExist_ThrowsTripNotFoundException()
    {
        var handler = new GetTripNotificationsQueryHandler(
            new FakeTripRepository(trip: null),
            new FakeNotificationRepository([]),
            new FakeUserRepository(new Dictionary<Guid, User>()),
            new FakeCurrentUserService(Guid.NewGuid()));

        await Assert.ThrowsAsync<TripNotFoundException>(() => handler.Handle(
            new GetTripNotificationsQuery { TripId = Guid.NewGuid() }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_AuthorNoLongerResolvable_ReturnsNullCreatedByName()
    {
        var organizationId = Guid.NewGuid();
        var trip = CreateTrip(organizationId, Guid.NewGuid());
        var notification = CreateNotification(organizationId, Guid.NewGuid());

        var handler = new GetTripNotificationsQueryHandler(
            new FakeTripRepository(trip),
            new FakeNotificationRepository([notification]),
            new FakeUserRepository(new Dictionary<Guid, User>()),
            new FakeCurrentUserService(organizationId));

        var result = await handler.Handle(
            new GetTripNotificationsQuery { TripId = trip.Id }, TestContext.Current.CancellationToken);

        Assert.Single(result);
        Assert.Null(result[0].CreatedByName);
    }
}
