using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Commands;
using Checkbus.ApiService.Domain.Entities.BoardingRecords;
using Checkbus.ApiService.Domain.Entities.Passengers;
using Checkbus.ApiService.Domain.Entities.Reservations;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using Microsoft.Extensions.Logging.Abstractions;

namespace Checkbus.Tests.Boarding;

/// <summary>
/// Proves <see cref="CloseBoardingCommandHandler"/> ("Cerrar embarque",
/// odd/tasks/reservas-publicas.md Fase 3 / A4, extended by CU-12): same IDOR-safe ownership
/// check as <see cref="GetTripBoardingListQueryHandlerTests"/>, delegates the bulk
/// Pendiente -> Ausente transition to <see cref="IBoardingRecordRepository.CloseBoardingAsync"/>,
/// and then notifies every passenger left Ausente by email.
/// </summary>
public class CloseBoardingCommandHandlerTests
{
    private sealed class FakeTripRepository(Trip? trip) : ITripRepository
    {
        public Task AddAsync(Trip trip, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CloseBoardingCommandHandler.");

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(trip);

        public Task UpdateAsync(Trip trip, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CloseBoardingCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CloseBoardingCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(Guid vehicleId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CloseBoardingCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(Guid driverId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CloseBoardingCommandHandler.");

        public Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CloseBoardingCommandHandler.");

        public Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CloseBoardingCommandHandler.");

        public Task UpdateStopDepartureTimesAsync(
            IReadOnlyDictionary<Guid, DateTime> departureTimesByStopId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CloseBoardingCommandHandler.");

        public Task<bool> SlugExistsAsync(Guid organizationId, string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CloseBoardingCommandHandler.");

        public Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CloseBoardingCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetPublicUpcomingByOrganizationIdAsync(
            Guid organizationId, DateTime? dateFrom, DateTime? dateTo, string? eventName, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CloseBoardingCommandHandler.");
    }

    private sealed class FakeBoardingRecordRepository(IReadOnlyList<BoardingRecord>? recordsAfterClose = null) : IBoardingRecordRepository
    {
        public Guid? ClosedTripId { get; private set; }

        public Task EnsureForTripAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CloseBoardingCommandHandler.");

        public Task<IReadOnlyList<BoardingRecord>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => Task.FromResult(recordsAfterClose ?? []);

        public Task<BoardingRecord?> GetByReservationIdAsync(Guid reservationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CloseBoardingCommandHandler.");

        public Task ValidateAsync(Guid reservationId, ValidationMethod method, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CloseBoardingCommandHandler.");

        public Task CloseBoardingAsync(Guid tripId, CancellationToken cancellationToken)
        {
            ClosedTripId = tripId;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeReservationRepository(IReadOnlyList<Reservation> reservations) : IReservationRepository
    {
        public Task<IReadOnlyList<Reservation>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => Task.FromResult(reservations);

        public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CloseBoardingCommandHandler.");

        public Task CreateBatchAsync(
            Checkbus.ApiService.Domain.Entities.Payments.Payment payment,
            IReadOnlyCollection<Passenger> passengers,
            IReadOnlyCollection<Reservation> reservations,
            CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CloseBoardingCommandHandler.");

        public Task CancelAsync(Guid reservationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CloseBoardingCommandHandler.");
    }

    private sealed class FakePassengerRepository(IReadOnlyDictionary<Guid, Passenger> passengersById) : IPassengerRepository
    {
        public Task<Passenger?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(passengersById.GetValueOrDefault(id));
    }

    private sealed class FakeEventRepository(Event? @event) : IEventRepository
    {
        public Task AddAsync(Event @event, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CloseBoardingCommandHandler.");

        public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(@event);

        public Task UpdateAsync(Event @event, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CloseBoardingCommandHandler.");

        public Task<IReadOnlyList<Event>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CloseBoardingCommandHandler.");
    }

    private sealed class FakeEmailService : IEmailService
    {
        public List<EmailMessage> SentMessages { get; } = [];
        public bool ThrowOnSend { get; set; }

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            if (ThrowOnSend)
            {
                throw new InvalidOperationException("Simulated SMTP failure.");
            }

            SentMessages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCurrentUserService(Guid? organizationId, Guid? userId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => userId;
        public Guid? OrganizationId => organizationId;
        public string? Role => "Chofer";
        public string? Email => "chofer@checkbus-demo.com";
    }

    private static Trip CreateTrip(Guid organizationId, Guid driverId, Guid? eventId = null) => new()
    {
        Id = Guid.NewGuid(),
        Slug = Guid.NewGuid().ToString(),
        OrganizationId = organizationId,
        DriverId = driverId,
        VehicleId = Guid.NewGuid(),
        EventId = eventId ?? Guid.NewGuid(),
        DepartureDate = DateTime.UtcNow,
        ArrivalDate = DateTime.UtcNow.AddHours(8),
        Capacity = 45,
        AvailableSeats = 44,
        Price = 15000m,
        Status = TripStatus.Publicado
    };

    private static Event CreateEvent(Guid id) => new()
    {
        Id = id,
        Name = "Final Copa Argentina",
        Type = EventType.Partido,
        Date = DateTime.UtcNow.AddDays(10),
        LocationId = Guid.NewGuid(),
        OrganizationId = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static Passenger CreatePassenger(Guid id, string email = "pasajero@example.com") => new()
    {
        Id = id,
        Name = "Juan",
        Surname = "Perez",
        DocumentType = DocumentType.DNI,
        DocumentNumber = "40123456",
        Email = email,
        Phone = "1122334455"
    };

    private static CloseBoardingCommandHandler BuildHandler(
        Trip? trip,
        FakeBoardingRecordRepository boardingRecordRepository,
        FakeReservationRepository? reservationRepository = null,
        FakePassengerRepository? passengerRepository = null,
        FakeEventRepository? eventRepository = null,
        FakeEmailService? emailService = null,
        Guid? organizationId = null,
        Guid? driverId = null) => new(
            new FakeTripRepository(trip),
            boardingRecordRepository,
            reservationRepository ?? new FakeReservationRepository([]),
            passengerRepository ?? new FakePassengerRepository(new Dictionary<Guid, Passenger>()),
            eventRepository ?? new FakeEventRepository(trip is null ? null : CreateEvent(trip.EventId)),
            emailService ?? new FakeEmailService(),
            new FakeCurrentUserService(organizationId ?? trip?.OrganizationId, driverId ?? trip?.DriverId),
            NullLogger<CloseBoardingCommandHandler>.Instance);

    [Fact]
    public async Task Handle_HappyPath_DelegatesToCloseBoardingAsyncForThisTrip()
    {
        var organizationId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var trip = CreateTrip(organizationId, driverId);
        var boardingRecordRepository = new FakeBoardingRecordRepository();

        var handler = BuildHandler(trip, boardingRecordRepository);

        await handler.Handle(new CloseBoardingCommand { TripId = trip.Id }, TestContext.Current.CancellationToken);

        Assert.Equal(trip.Id, boardingRecordRepository.ClosedTripId);
    }

    [Fact]
    public async Task Handle_TripNotFound_ThrowsTripNotFound()
    {
        var handler = BuildHandler(null, new FakeBoardingRecordRepository(), organizationId: Guid.NewGuid(), driverId: Guid.NewGuid());

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(new CloseBoardingCommand { TripId = Guid.NewGuid() }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_TripBelongsToDifferentDriver_ThrowsTripNotFound()
    {
        var organizationId = Guid.NewGuid();
        var trip = CreateTrip(organizationId, Guid.NewGuid());

        var handler = BuildHandler(trip, new FakeBoardingRecordRepository(), organizationId: organizationId, driverId: Guid.NewGuid());

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(new CloseBoardingCommand { TripId = trip.Id }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_TripBelongsToDifferentOrganization_ThrowsTripNotFound()
    {
        var driverId = Guid.NewGuid();
        var trip = CreateTrip(Guid.NewGuid(), driverId);

        var handler = BuildHandler(trip, new FakeBoardingRecordRepository(), organizationId: Guid.NewGuid(), driverId: driverId);

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(new CloseBoardingCommand { TripId = trip.Id }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_PassengersLeftAusente_SendsNotificationEmailToEachOne()
    {
        var organizationId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var trip = CreateTrip(organizationId, driverId);
        var absentReservationId = Guid.NewGuid();
        var boardedReservationId = Guid.NewGuid();
        var absentPassengerId = Guid.NewGuid();
        var boardedPassengerId = Guid.NewGuid();

        var boardingRecordRepository = new FakeBoardingRecordRepository(
        [
            new BoardingRecord { Id = Guid.NewGuid(), ReservationId = absentReservationId, Status = BoardingStatus.Ausente },
            new BoardingRecord { Id = Guid.NewGuid(), ReservationId = boardedReservationId, Status = BoardingStatus.Abordado }
        ]);
        var reservationRepository = new FakeReservationRepository(
        [
            new Reservation { Id = absentReservationId, TripId = trip.Id, SeatId = Guid.NewGuid(), PassengerId = absentPassengerId, Price = trip.Price },
            new Reservation { Id = boardedReservationId, TripId = trip.Id, SeatId = Guid.NewGuid(), PassengerId = boardedPassengerId, Price = trip.Price }
        ]);
        var passengerRepository = new FakePassengerRepository(new Dictionary<Guid, Passenger>
        {
            [absentPassengerId] = CreatePassenger(absentPassengerId, "ausente@example.com"),
            [boardedPassengerId] = CreatePassenger(boardedPassengerId, "abordado@example.com")
        });
        var emailService = new FakeEmailService();

        var handler = BuildHandler(trip, boardingRecordRepository, reservationRepository, passengerRepository, emailService: emailService);

        await handler.Handle(new CloseBoardingCommand { TripId = trip.Id }, TestContext.Current.CancellationToken);

        Assert.Single(emailService.SentMessages);
        Assert.Equal("ausente@example.com", emailService.SentMessages[0].To);
    }

    [Fact]
    public async Task Handle_NoAbsentPassengers_SendsNoEmail()
    {
        var organizationId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var trip = CreateTrip(organizationId, driverId);
        var boardingRecordRepository = new FakeBoardingRecordRepository(
        [
            new BoardingRecord { Id = Guid.NewGuid(), ReservationId = Guid.NewGuid(), Status = BoardingStatus.Abordado }
        ]);
        var emailService = new FakeEmailService();

        var handler = BuildHandler(trip, boardingRecordRepository, emailService: emailService);

        await handler.Handle(new CloseBoardingCommand { TripId = trip.Id }, TestContext.Current.CancellationToken);

        Assert.Empty(emailService.SentMessages);
    }

    [Fact]
    public async Task Handle_EmailSendFails_CloseBoardingStillSucceeds()
    {
        var organizationId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var trip = CreateTrip(organizationId, driverId);
        var absentReservationId = Guid.NewGuid();
        var absentPassengerId = Guid.NewGuid();

        var boardingRecordRepository = new FakeBoardingRecordRepository(
        [
            new BoardingRecord { Id = Guid.NewGuid(), ReservationId = absentReservationId, Status = BoardingStatus.Ausente }
        ]);
        var reservationRepository = new FakeReservationRepository(
        [
            new Reservation { Id = absentReservationId, TripId = trip.Id, SeatId = Guid.NewGuid(), PassengerId = absentPassengerId, Price = trip.Price }
        ]);
        var passengerRepository = new FakePassengerRepository(new Dictionary<Guid, Passenger>
        {
            [absentPassengerId] = CreatePassenger(absentPassengerId)
        });
        var emailService = new FakeEmailService { ThrowOnSend = true };

        var handler = BuildHandler(trip, boardingRecordRepository, reservationRepository, passengerRepository, emailService: emailService);

        await handler.Handle(new CloseBoardingCommand { TripId = trip.Id }, TestContext.Current.CancellationToken);

        Assert.Equal(trip.Id, boardingRecordRepository.ClosedTripId);
    }
}
