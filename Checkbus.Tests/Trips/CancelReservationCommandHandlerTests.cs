using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Commands;
using Checkbus.ApiService.Domain.Entities.BoardingRecords;
using Checkbus.ApiService.Domain.Entities.Passengers;
using Checkbus.ApiService.Domain.Entities.Reservations;
using Checkbus.ApiService.Domain.Entities.Seats;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Reservations;
using Microsoft.Extensions.Logging.Abstractions;

namespace Checkbus.Tests.Trips;

/// <summary>
/// Proves <see cref="CancelReservationCommandHandler"/> (odd/tasks/reservas-publicas.md
/// Fase 4 / C2, C4, C6): IDOR-safe load-by-id shape with NO ownership check (public surface,
/// no caller identity), the two independent cancellation-blocking reasons (Abordado /
/// inside-24h), delegation of the actual Status flip to
/// <see cref="IReservationRepository.CancelAsync"/>, and the best-effort cancellation email
/// (sent after success, never propagates a failure). BoardingRecord itself must never be
/// touched by cancellation. Fake-repository style, matching
/// <see cref="ValidateBoardingCommandHandlerTests"/>/<c>CreatePublicReservationCommandHandlerTests</c>.
/// </summary>
public class CancelReservationCommandHandlerTests
{
    private sealed class FakeReservationRepository(Reservation? reservation) : IReservationRepository
    {
        public Guid? CancelledReservationId { get; private set; }

        public Task<IReadOnlyList<Reservation>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");

        public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(reservation);

        public Task CreateBatchAsync(IReadOnlyCollection<Passenger> passengers, IReadOnlyCollection<Reservation> reservations, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");

        public Task CancelAsync(Guid reservationId, CancellationToken cancellationToken)
        {
            CancelledReservationId = reservationId;
            if (reservation is not null)
            {
                reservation.Status = ReservationStatus.Cancelada;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeTripRepository(Trip? trip) : ITripRepository
    {
        public Task AddAsync(Trip trip, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(trip);

        public Task UpdateAsync(Trip trip, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(Guid vehicleId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(Guid driverId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");

        public Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");

        public Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");

        public Task UpdateStopDepartureTimesAsync(
            IReadOnlyDictionary<Guid, DateTime> departureTimesByStopId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");
    }

    private sealed class FakeBoardingRecordRepository(BoardingRecord? record) : IBoardingRecordRepository
    {
        public Task EnsureForTripAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");

        public Task<IReadOnlyList<BoardingRecord>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");

        public Task<BoardingRecord?> GetByReservationIdAsync(Guid reservationId, CancellationToken cancellationToken)
            => Task.FromResult(record);

        public Task ValidateAsync(Guid reservationId, ValidationMethod method, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");

        public Task CloseBoardingAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");
    }

    private sealed class FakePassengerRepository(Passenger? passenger) : IPassengerRepository
    {
        public Task<Passenger?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(passenger);
    }

    private sealed class FakeSeatRepository(IReadOnlyList<Seat> seats) : ISeatRepository
    {
        public Task EnsureSeatsForVehicleAsync(Guid vehicleId, int capacity, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");

        public Task<IReadOnlyList<Seat>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken)
            => Task.FromResult(seats);
    }

    private sealed class FakeEmailService(bool throwOnSend = false) : IEmailService
    {
        public List<EmailMessage> SentMessages { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            if (throwOnSend)
            {
                throw new InvalidOperationException("Simulated SMTP failure.");
            }

            SentMessages.Add(message);
            return Task.CompletedTask;
        }
    }

    private static Trip CreateTrip(DateTime departureDate) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = Guid.NewGuid(),
        DriverId = Guid.NewGuid(),
        VehicleId = Guid.NewGuid(),
        EventId = Guid.NewGuid(),
        DepartureDate = departureDate,
        ArrivalDate = departureDate.AddHours(4),
        Capacity = 45,
        AvailableSeats = 44,
        Price = 15000m,
        Status = TripStatus.Publicado
    };

    private static Reservation CreateReservation(Guid tripId, Guid seatId, Guid passengerId) => new()
    {
        Id = Guid.NewGuid(),
        TripId = tripId,
        SeatId = seatId,
        PassengerId = passengerId,
        Price = 15000m
    };

    private static Passenger CreatePassenger(string email = "juan@checkbus.test") => new()
    {
        Id = Guid.NewGuid(),
        Name = "Juan",
        Surname = "Perez",
        DocumentType = DocumentType.DNI,
        DocumentNumber = "30111222",
        Email = email,
        Phone = "+54 11 5555-5555"
    };

    private static CancelReservationCommandHandler CreateHandler(
        FakeReservationRepository reservationRepository,
        Trip? trip,
        BoardingRecord? boardingRecord = null,
        Passenger? passenger = null,
        IReadOnlyList<Seat>? seats = null,
        FakeEmailService? emailService = null)
    {
        return new CancelReservationCommandHandler(
            reservationRepository,
            new FakeTripRepository(trip),
            new FakeBoardingRecordRepository(boardingRecord),
            new FakePassengerRepository(passenger),
            new FakeSeatRepository(seats ?? []),
            emailService ?? new FakeEmailService(),
            NullLogger<CancelReservationCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ReservationNotFound_ThrowsReservationNotFound()
    {
        var handler = CreateHandler(new FakeReservationRepository(null), null);

        await Assert.ThrowsAsync<ReservationNotFoundException>(() =>
            handler.Handle(
                new CancelReservationCommand { ReservationId = Guid.NewGuid() },
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_BoardingRecordAlreadyAbordado_ThrowsReservationNotCancellable()
    {
        var trip = CreateTrip(DateTime.UtcNow.AddDays(10));
        var reservation = CreateReservation(trip.Id, Guid.NewGuid(), Guid.NewGuid());
        var boardingRecord = new BoardingRecord { Id = Guid.NewGuid(), ReservationId = reservation.Id, Status = BoardingStatus.Abordado };
        var reservationRepository = new FakeReservationRepository(reservation);

        var handler = CreateHandler(reservationRepository, trip, boardingRecord);

        await Assert.ThrowsAsync<ReservationNotCancellableException>(() =>
            handler.Handle(
                new CancelReservationCommand { ReservationId = reservation.Id },
                TestContext.Current.CancellationToken));

        Assert.Null(reservationRepository.CancelledReservationId);
    }

    [Fact]
    public async Task Handle_LessThan24HoursBeforeDeparture_ThrowsReservationNotCancellable()
    {
        var trip = CreateTrip(DateTime.UtcNow.AddHours(5));
        var reservation = CreateReservation(trip.Id, Guid.NewGuid(), Guid.NewGuid());
        var boardingRecord = new BoardingRecord { Id = Guid.NewGuid(), ReservationId = reservation.Id, Status = BoardingStatus.Pendiente };
        var reservationRepository = new FakeReservationRepository(reservation);

        var handler = CreateHandler(reservationRepository, trip, boardingRecord);

        await Assert.ThrowsAsync<ReservationNotCancellableException>(() =>
            handler.Handle(
                new CancelReservationCommand { ReservationId = reservation.Id },
                TestContext.Current.CancellationToken));

        Assert.Null(reservationRepository.CancelledReservationId);
    }

    [Fact]
    public async Task Handle_HappyPath_CancelsReservation_AndLeavesPendienteBoardingRecordUntouched()
    {
        var seatId = Guid.NewGuid();
        var trip = CreateTrip(DateTime.UtcNow.AddDays(10));
        var reservation = CreateReservation(trip.Id, seatId, Guid.NewGuid());
        var boardingRecord = new BoardingRecord { Id = Guid.NewGuid(), ReservationId = reservation.Id, Status = BoardingStatus.Pendiente };
        var seats = new List<Seat> { new() { Id = seatId, VehicleId = trip.VehicleId, Number = 7 } };
        var reservationRepository = new FakeReservationRepository(reservation);

        var handler = CreateHandler(reservationRepository, trip, boardingRecord, passenger: null, seats);

        await handler.Handle(
            new CancelReservationCommand { ReservationId = reservation.Id },
            TestContext.Current.CancellationToken);

        Assert.Equal(reservation.Id, reservationRepository.CancelledReservationId);
        Assert.Equal(ReservationStatus.Cancelada, reservation.Status);

        // Cancellation must NEVER touch BoardingRecord — not deleted, not altered.
        Assert.Equal(BoardingStatus.Pendiente, boardingRecord.Status);
    }

    [Fact]
    public async Task Handle_AusenteBoardingRecord_DoesNotBlockCancellation()
    {
        // Only Abordado blocks cancellation — Pendiente/Ausente do not.
        var seatId = Guid.NewGuid();
        var trip = CreateTrip(DateTime.UtcNow.AddDays(10));
        var reservation = CreateReservation(trip.Id, seatId, Guid.NewGuid());
        var boardingRecord = new BoardingRecord { Id = Guid.NewGuid(), ReservationId = reservation.Id, Status = BoardingStatus.Ausente };
        var reservationRepository = new FakeReservationRepository(reservation);

        var handler = CreateHandler(reservationRepository, trip, boardingRecord);

        await handler.Handle(
            new CancelReservationCommand { ReservationId = reservation.Id },
            TestContext.Current.CancellationToken);

        Assert.Equal(reservation.Id, reservationRepository.CancelledReservationId);
        Assert.Equal(BoardingStatus.Ausente, boardingRecord.Status);
    }

    [Fact]
    public async Task Handle_HappyPath_WithPassengerEmail_SendsCancellationEmail()
    {
        var seatId = Guid.NewGuid();
        var trip = CreateTrip(DateTime.UtcNow.AddDays(10));
        var reservation = CreateReservation(trip.Id, seatId, Guid.NewGuid());
        var passenger = CreatePassenger();
        var seats = new List<Seat> { new() { Id = seatId, VehicleId = trip.VehicleId, Number = 7 } };
        var emailService = new FakeEmailService();
        var reservationRepository = new FakeReservationRepository(reservation);

        var handler = CreateHandler(reservationRepository, trip, boardingRecord: null, passenger, seats, emailService);

        await handler.Handle(
            new CancelReservationCommand { ReservationId = reservation.Id },
            TestContext.Current.CancellationToken);

        Assert.Single(emailService.SentMessages);
        Assert.Equal(passenger.Email, emailService.SentMessages[0].To);
    }

    [Fact]
    public async Task Handle_EmailServiceThrows_DoesNotPropagate_CancellationStillSucceeds()
    {
        var seatId = Guid.NewGuid();
        var trip = CreateTrip(DateTime.UtcNow.AddDays(10));
        var reservation = CreateReservation(trip.Id, seatId, Guid.NewGuid());
        var passenger = CreatePassenger();
        var seats = new List<Seat> { new() { Id = seatId, VehicleId = trip.VehicleId, Number = 7 } };
        var emailService = new FakeEmailService(throwOnSend: true);
        var reservationRepository = new FakeReservationRepository(reservation);

        var handler = CreateHandler(reservationRepository, trip, boardingRecord: null, passenger, seats, emailService);

        // Must not throw — the email failure is logged and swallowed, the already-persisted
        // cancellation is unaffected.
        await handler.Handle(
            new CancelReservationCommand { ReservationId = reservation.Id },
            TestContext.Current.CancellationToken);

        Assert.Equal(reservation.Id, reservationRepository.CancelledReservationId);
        Assert.Empty(emailService.SentMessages);
    }

    [Fact]
    public async Task Handle_PassengerHasNoEmail_DoesNotAttemptToSend()
    {
        var seatId = Guid.NewGuid();
        var trip = CreateTrip(DateTime.UtcNow.AddDays(10));
        var reservation = CreateReservation(trip.Id, seatId, Guid.NewGuid());
        var passenger = CreatePassenger(email: "");
        var seats = new List<Seat> { new() { Id = seatId, VehicleId = trip.VehicleId, Number = 7 } };
        var emailService = new FakeEmailService();
        var reservationRepository = new FakeReservationRepository(reservation);

        var handler = CreateHandler(reservationRepository, trip, boardingRecord: null, passenger, seats, emailService);

        await handler.Handle(
            new CancelReservationCommand { ReservationId = reservation.Id },
            TestContext.Current.CancellationToken);

        Assert.Empty(emailService.SentMessages);
    }
}
