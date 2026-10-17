using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Notifications.Commands;
using Checkbus.ApiService.Domain.Entities.Notifications;
using Checkbus.ApiService.Domain.Entities.Passengers;
using Checkbus.ApiService.Domain.Entities.Payments;
using Checkbus.ApiService.Domain.Entities.Reservations;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using Microsoft.Extensions.Logging.Abstractions;

namespace Checkbus.Tests.Notifications;

/// <summary>
/// Proves <see cref="CreateNotificationCommandHandler"/> (CU-04): scope resolution (Trip vs
/// Event), the Confirmada-only audience filter, persisting the Notification row BEFORE any
/// email send, the defensive per-recipient catch-and-log loop (one failing recipient never
/// aborts the others), and the IDOR-safe cross-tenant rejection for both target types.
/// </summary>
public class CreateNotificationCommandHandlerTests
{
    private sealed class FakeCurrentUserService(Guid? organizationId, Guid? userId = null) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => userId ?? Guid.NewGuid();
        public Guid? OrganizationId => organizationId;
        public string? Role => "Administrador";
        public string? Email => "admin@checkbus-demo.com";
    }

    private sealed class FakeTripRepository(
        IReadOnlyDictionary<Guid, Trip> tripsById, IReadOnlyList<Trip>? tripsByEvent = null) : ITripRepository
    {
        public Task AddAsync(Trip t, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateNotificationCommandHandler.");

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(tripsById.TryGetValue(id, out var trip) ? trip : null);

        public Task UpdateAsync(Trip t, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateNotificationCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateNotificationCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(Guid vehicleId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateNotificationCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(Guid driverId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateNotificationCommandHandler.");

        public Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateNotificationCommandHandler.");

        public Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateNotificationCommandHandler.");

        public Task UpdateStopDepartureTimesAsync(
            IReadOnlyDictionary<Guid, DateTime> departureTimesByStopId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateNotificationCommandHandler.");

        public Task<bool> SlugExistsAsync(Guid organizationId, string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateNotificationCommandHandler.");

        public Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateNotificationCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetPublicUpcomingByOrganizationIdAsync(
            Guid organizationId, DateTime? dateFrom, DateTime? dateTo, string? eventName, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateNotificationCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken)
            => Task.FromResult(tripsByEvent ?? []);
    }

    private sealed class FakeEventRepository(Event? @event) : IEventRepository
    {
        public Task AddAsync(Event e, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateNotificationCommandHandler.");

        public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(@event);

        public Task UpdateAsync(Event e, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateNotificationCommandHandler.");

        public Task<IReadOnlyList<Event>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateNotificationCommandHandler.");
    }

    private sealed class FakeReservationRepository(IReadOnlyDictionary<Guid, IReadOnlyList<Reservation>> reservationsByTripId)
        : IReservationRepository
    {
        public Task<IReadOnlyList<Reservation>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => Task.FromResult(reservationsByTripId.TryGetValue(tripId, out var reservations) ? reservations : []);

        public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateNotificationCommandHandler.");

        public Task CreateBatchAsync(Payment payment, IReadOnlyCollection<Passenger> passengers, IReadOnlyCollection<Reservation> batchReservations, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateNotificationCommandHandler.");

        public Task CancelAsync(Guid reservationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateNotificationCommandHandler.");
    }

    private sealed class FakePassengerRepository(IReadOnlyDictionary<Guid, Passenger> passengersById) : IPassengerRepository
    {
        public Task<Passenger?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(passengersById.TryGetValue(id, out var passenger) ? passenger : null);
    }

    private sealed class FakeNotificationRepository : INotificationRepository
    {
        public List<Notification> AddedNotifications { get; } = [];

        public Task AddAsync(Notification notification, CancellationToken cancellationToken)
        {
            AddedNotifications.Add(notification);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Notification>> GetRelevantToTripAsync(Guid tripId, Guid eventId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateNotificationCommandHandler.");
    }

    private sealed class FakeEmailService(ISet<string>? emailsThatThrow = null) : IEmailService
    {
        public List<EmailMessage> SentMessages { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            if (emailsThatThrow?.Contains(message.To) == true)
            {
                throw new InvalidOperationException("Simulated SMTP failure.");
            }

            SentMessages.Add(message);
            return Task.CompletedTask;
        }
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

    private static Event CreateEvent(Guid organizationId) => new()
    {
        Id = Guid.NewGuid(),
        Name = "River vs Boca",
        Type = EventType.Partido,
        Date = DateTime.UtcNow.AddDays(5),
        LocationId = Guid.NewGuid(),
        OrganizationId = organizationId
    };

    private static Reservation CreateReservation(
        Guid tripId, Guid passengerId, ReservationStatus status = ReservationStatus.Confirmada) => new()
    {
        Id = Guid.NewGuid(),
        TripId = tripId,
        SeatId = Guid.NewGuid(),
        PassengerId = passengerId,
        Price = 15000m,
        Status = status
    };

    private static Passenger CreatePassenger(string email) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Juan",
        Surname = "Perez",
        DocumentType = DocumentType.DNI,
        DocumentNumber = "30111222",
        Email = email,
        Phone = "+54 11 5555-5555"
    };

    [Fact]
    public async Task Handle_TripScope_SendsOnlyToThatTripsConfirmadaPassengers()
    {
        var organizationId = Guid.NewGuid();
        var trip = CreateTrip(organizationId, Guid.NewGuid());
        var confirmedPassenger = CreatePassenger("confirmada@checkbus.test");
        var confirmedReservation = CreateReservation(trip.Id, confirmedPassenger.Id, ReservationStatus.Confirmada);
        var passengersById = new Dictionary<Guid, Passenger> { [confirmedPassenger.Id] = confirmedPassenger };
        var reservationsByTripId = new Dictionary<Guid, IReadOnlyList<Reservation>> { [trip.Id] = [confirmedReservation] };
        var emailService = new FakeEmailService();
        var notificationRepository = new FakeNotificationRepository();

        var handler = new CreateNotificationCommandHandler(
            new FakeTripRepository(new Dictionary<Guid, Trip> { [trip.Id] = trip }),
            new FakeEventRepository(@event: null),
            new FakeReservationRepository(reservationsByTripId),
            new FakePassengerRepository(passengersById),
            notificationRepository,
            emailService,
            new FakeCurrentUserService(organizationId),
            NullLogger<CreateNotificationCommandHandler>.Instance);

        await handler.Handle(
            new CreateNotificationCommand
            {
                TargetType = NotificationTargetType.Trip,
                TargetId = trip.Id,
                Reason = NotificationReason.CambioHorario,
                Message = "El horario cambió."
            },
            TestContext.Current.CancellationToken);

        Assert.Single(emailService.SentMessages);
        Assert.Equal(confirmedPassenger.Email, emailService.SentMessages[0].To);
        Assert.Single(notificationRepository.AddedNotifications);
        Assert.Equal(NotificationTargetType.Trip, notificationRepository.AddedNotifications[0].TargetType);
        Assert.Equal(trip.Id, notificationRepository.AddedNotifications[0].TargetId);
    }

    [Fact]
    public async Task Handle_EventScope_SendsToConfirmadaPassengersAcrossEveryTripOfThatEvent()
    {
        var organizationId = Guid.NewGuid();
        var @event = CreateEvent(organizationId);
        var trip1 = CreateTrip(organizationId, @event.Id);
        var trip2 = CreateTrip(organizationId, @event.Id);
        var passenger1 = CreatePassenger("p1@checkbus.test");
        var passenger2 = CreatePassenger("p2@checkbus.test");
        var reservation1 = CreateReservation(trip1.Id, passenger1.Id);
        var reservation2 = CreateReservation(trip2.Id, passenger2.Id);
        var passengersById = new Dictionary<Guid, Passenger> { [passenger1.Id] = passenger1, [passenger2.Id] = passenger2 };
        var reservationsByTripId = new Dictionary<Guid, IReadOnlyList<Reservation>>
        {
            [trip1.Id] = [reservation1],
            [trip2.Id] = [reservation2]
        };
        var emailService = new FakeEmailService();

        var handler = new CreateNotificationCommandHandler(
            new FakeTripRepository(new Dictionary<Guid, Trip>(), tripsByEvent: [trip1, trip2]),
            new FakeEventRepository(@event),
            new FakeReservationRepository(reservationsByTripId),
            new FakePassengerRepository(passengersById),
            new FakeNotificationRepository(),
            emailService,
            new FakeCurrentUserService(organizationId),
            NullLogger<CreateNotificationCommandHandler>.Instance);

        await handler.Handle(
            new CreateNotificationCommand
            {
                TargetType = NotificationTargetType.Event,
                TargetId = @event.Id,
                Reason = NotificationReason.CambioVehiculo,
                Message = "Cambió el micro asignado."
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(2, emailService.SentMessages.Count);
        Assert.Contains(emailService.SentMessages, m => m.To == passenger1.Email);
        Assert.Contains(emailService.SentMessages, m => m.To == passenger2.Email);
    }

    [Theory]
    [InlineData(ReservationStatus.PendientePago)]
    [InlineData(ReservationStatus.Cancelada)]
    public async Task Handle_NonConfirmadaReservation_PassengerIsNeverNotified(ReservationStatus status)
    {
        var organizationId = Guid.NewGuid();
        var trip = CreateTrip(organizationId, Guid.NewGuid());
        var passenger = CreatePassenger("nopago@checkbus.test");
        var reservation = CreateReservation(trip.Id, passenger.Id, status);
        var passengersById = new Dictionary<Guid, Passenger> { [passenger.Id] = passenger };
        var reservationsByTripId = new Dictionary<Guid, IReadOnlyList<Reservation>> { [trip.Id] = [reservation] };
        var emailService = new FakeEmailService();

        var handler = new CreateNotificationCommandHandler(
            new FakeTripRepository(new Dictionary<Guid, Trip> { [trip.Id] = trip }),
            new FakeEventRepository(@event: null),
            new FakeReservationRepository(reservationsByTripId),
            new FakePassengerRepository(passengersById),
            new FakeNotificationRepository(),
            emailService,
            new FakeCurrentUserService(organizationId),
            NullLogger<CreateNotificationCommandHandler>.Instance);

        await handler.Handle(
            new CreateNotificationCommand
            {
                TargetType = NotificationTargetType.Trip,
                TargetId = trip.Id,
                Reason = NotificationReason.Otro,
                Message = "Aviso."
            },
            TestContext.Current.CancellationToken);

        Assert.Empty(emailService.SentMessages);
    }

    [Fact]
    public async Task Handle_NotificationIsPersisted_EvenWhenEveryEmailSendFails()
    {
        var organizationId = Guid.NewGuid();
        var trip = CreateTrip(organizationId, Guid.NewGuid());
        var passenger = CreatePassenger("fails@checkbus.test");
        var reservation = CreateReservation(trip.Id, passenger.Id);
        var passengersById = new Dictionary<Guid, Passenger> { [passenger.Id] = passenger };
        var reservationsByTripId = new Dictionary<Guid, IReadOnlyList<Reservation>> { [trip.Id] = [reservation] };
        var emailService = new FakeEmailService(emailsThatThrow: new HashSet<string> { passenger.Email });
        var notificationRepository = new FakeNotificationRepository();

        var handler = new CreateNotificationCommandHandler(
            new FakeTripRepository(new Dictionary<Guid, Trip> { [trip.Id] = trip }),
            new FakeEventRepository(@event: null),
            new FakeReservationRepository(reservationsByTripId),
            new FakePassengerRepository(passengersById),
            notificationRepository,
            emailService,
            new FakeCurrentUserService(organizationId),
            NullLogger<CreateNotificationCommandHandler>.Instance);

        await handler.Handle(
            new CreateNotificationCommand
            {
                TargetType = NotificationTargetType.Trip,
                TargetId = trip.Id,
                Reason = NotificationReason.Otro,
                Message = "Aviso."
            },
            TestContext.Current.CancellationToken);

        Assert.Single(notificationRepository.AddedNotifications);
        Assert.Empty(emailService.SentMessages);
    }

    [Fact]
    public async Task Handle_OneRecipientFails_OtherRecipientsStillGetTheirEmail()
    {
        var organizationId = Guid.NewGuid();
        var trip = CreateTrip(organizationId, Guid.NewGuid());
        var failingPassenger = CreatePassenger("fails@checkbus.test");
        var okPassenger = CreatePassenger("ok@checkbus.test");
        var failingReservation = CreateReservation(trip.Id, failingPassenger.Id);
        var okReservation = CreateReservation(trip.Id, okPassenger.Id);
        var passengersById = new Dictionary<Guid, Passenger>
        {
            [failingPassenger.Id] = failingPassenger,
            [okPassenger.Id] = okPassenger
        };
        var reservationsByTripId = new Dictionary<Guid, IReadOnlyList<Reservation>>
        {
            [trip.Id] = [failingReservation, okReservation]
        };
        var emailService = new FakeEmailService(emailsThatThrow: new HashSet<string> { failingPassenger.Email });

        var handler = new CreateNotificationCommandHandler(
            new FakeTripRepository(new Dictionary<Guid, Trip> { [trip.Id] = trip }),
            new FakeEventRepository(@event: null),
            new FakeReservationRepository(reservationsByTripId),
            new FakePassengerRepository(passengersById),
            new FakeNotificationRepository(),
            emailService,
            new FakeCurrentUserService(organizationId),
            NullLogger<CreateNotificationCommandHandler>.Instance);

        await handler.Handle(
            new CreateNotificationCommand
            {
                TargetType = NotificationTargetType.Trip,
                TargetId = trip.Id,
                Reason = NotificationReason.Otro,
                Message = "Aviso."
            },
            TestContext.Current.CancellationToken);

        Assert.Single(emailService.SentMessages);
        Assert.Equal(okPassenger.Email, emailService.SentMessages[0].To);
    }

    [Fact]
    public async Task Handle_TripTargetFromAnotherOrganization_ThrowsTripNotFoundException()
    {
        var trip = CreateTrip(Guid.NewGuid(), Guid.NewGuid());

        var handler = new CreateNotificationCommandHandler(
            new FakeTripRepository(new Dictionary<Guid, Trip> { [trip.Id] = trip }),
            new FakeEventRepository(@event: null),
            new FakeReservationRepository(new Dictionary<Guid, IReadOnlyList<Reservation>>()),
            new FakePassengerRepository(new Dictionary<Guid, Passenger>()),
            new FakeNotificationRepository(),
            new FakeEmailService(),
            new FakeCurrentUserService(Guid.NewGuid()),
            NullLogger<CreateNotificationCommandHandler>.Instance);

        await Assert.ThrowsAsync<TripNotFoundException>(() => handler.Handle(
            new CreateNotificationCommand
            {
                TargetType = NotificationTargetType.Trip,
                TargetId = trip.Id,
                Reason = NotificationReason.Otro,
                Message = "Aviso."
            },
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_EventTargetFromAnotherOrganization_ThrowsEventNotFoundException()
    {
        var @event = CreateEvent(Guid.NewGuid());

        var handler = new CreateNotificationCommandHandler(
            new FakeTripRepository(new Dictionary<Guid, Trip>()),
            new FakeEventRepository(@event),
            new FakeReservationRepository(new Dictionary<Guid, IReadOnlyList<Reservation>>()),
            new FakePassengerRepository(new Dictionary<Guid, Passenger>()),
            new FakeNotificationRepository(),
            new FakeEmailService(),
            new FakeCurrentUserService(Guid.NewGuid()),
            NullLogger<CreateNotificationCommandHandler>.Instance);

        await Assert.ThrowsAsync<EventNotFoundException>(() => handler.Handle(
            new CreateNotificationCommand
            {
                TargetType = NotificationTargetType.Event,
                TargetId = @event.Id,
                Reason = NotificationReason.Otro,
                Message = "Aviso."
            },
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_TripTargetDoesNotExist_ThrowsTripNotFoundException()
    {
        var handler = new CreateNotificationCommandHandler(
            new FakeTripRepository(new Dictionary<Guid, Trip>()),
            new FakeEventRepository(@event: null),
            new FakeReservationRepository(new Dictionary<Guid, IReadOnlyList<Reservation>>()),
            new FakePassengerRepository(new Dictionary<Guid, Passenger>()),
            new FakeNotificationRepository(),
            new FakeEmailService(),
            new FakeCurrentUserService(Guid.NewGuid()),
            NullLogger<CreateNotificationCommandHandler>.Instance);

        await Assert.ThrowsAsync<TripNotFoundException>(() => handler.Handle(
            new CreateNotificationCommand
            {
                TargetType = NotificationTargetType.Trip,
                TargetId = Guid.NewGuid(),
                Reason = NotificationReason.Otro,
                Message = "Aviso."
            },
            TestContext.Current.CancellationToken));
    }
}
