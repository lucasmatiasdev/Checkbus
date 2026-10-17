using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Commands;
using Checkbus.ApiService.Domain.Entities.BoardingRecords;
using Checkbus.ApiService.Domain.Entities.Passengers;
using Checkbus.ApiService.Domain.Entities.Payments;
using Checkbus.ApiService.Domain.Entities.Reservations;
using Checkbus.ApiService.Domain.Entities.Seats;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using Microsoft.Extensions.Logging.Abstractions;

namespace Checkbus.Tests.Trips;

/// <summary>
/// Proves <see cref="FinishTripCommandHandler"/> (CU-08): tenant-scoped IDOR-safe load, the
/// invalid-status rejection, that it closes boarding (Pendiente -> Ausente) as part of
/// finishing, the Completado transition with ActualArrivalDate/Observations persisted, and the
/// best-effort per-passenger "trip finished" email.
/// </summary>
public class FinishTripCommandHandlerTests
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
        public Trip? UpdatedTrip { get; private set; }
        public int UpdateCallCount { get; private set; }

        public Task AddAsync(Trip t, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by FinishTripCommandHandler.");

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(trip);

        public Task UpdateAsync(Trip t, CancellationToken cancellationToken)
        {
            UpdateCallCount++;
            UpdatedTrip = t;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by FinishTripCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(Guid vehicleId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by FinishTripCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(Guid driverId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by FinishTripCommandHandler.");

        public Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by FinishTripCommandHandler.");

        public Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by FinishTripCommandHandler.");

        public Task UpdateStopDepartureTimesAsync(
            IReadOnlyDictionary<Guid, DateTime> departureTimesByStopId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by FinishTripCommandHandler.");

        public Task<bool> SlugExistsAsync(Guid organizationId, string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by FinishTripCommandHandler.");

        public Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by FinishTripCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetPublicUpcomingByOrganizationIdAsync(
            Guid organizationId, DateTime? dateFrom, DateTime? dateTo, string? eventName, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by FinishTripCommandHandler.");
    }

    private sealed class FakeBoardingRecordRepository : IBoardingRecordRepository
    {
        public List<Guid> ClosedTripIds { get; } = [];

        public Task EnsureForTripAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by FinishTripCommandHandler.");

        public Task<IReadOnlyList<BoardingRecord>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by FinishTripCommandHandler.");

        public Task<BoardingRecord?> GetByReservationIdAsync(Guid reservationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by FinishTripCommandHandler.");

        public Task ValidateAsync(Guid reservationId, ValidationMethod method, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by FinishTripCommandHandler.");

        public Task CloseBoardingAsync(Guid tripId, CancellationToken cancellationToken)
        {
            ClosedTripIds.Add(tripId);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeReservationRepository(IReadOnlyList<Reservation> reservations) : IReservationRepository
    {
        public Task<IReadOnlyList<Reservation>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => Task.FromResult(reservations);

        public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by FinishTripCommandHandler.");

        public Task CreateBatchAsync(Payment payment, IReadOnlyCollection<Passenger> passengers, IReadOnlyCollection<Reservation> batchReservations, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by FinishTripCommandHandler.");

        public Task CancelAsync(Guid reservationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by FinishTripCommandHandler.");

        public Task<IReadOnlyList<Reservation>> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by FinishTripCommandHandler.");

        public Task<IReadOnlyList<Reservation>> GetAllByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by FinishTripCommandHandler.");
    }

    private sealed class FakePassengerRepository(IReadOnlyDictionary<Guid, Passenger> passengersById) : IPassengerRepository
    {
        public Task<Passenger?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(passengersById.TryGetValue(id, out var passenger) ? passenger : null);
    }

    private sealed class FakeSeatRepository(IReadOnlyList<Seat> seats) : ISeatRepository
    {
        public Task EnsureSeatsForVehicleAsync(Guid vehicleId, int capacity, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by FinishTripCommandHandler.");

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

    private static Trip CreateTrip(TripStatus status = TripStatus.EnProceso, Guid? organizationId = null) => new()
    {
        Id = Guid.NewGuid(),
        Slug = Guid.NewGuid().ToString(),
        OrganizationId = organizationId ?? Guid.NewGuid(),
        DriverId = Guid.NewGuid(),
        VehicleId = Guid.NewGuid(),
        EventId = Guid.NewGuid(),
        DepartureDate = DateTime.UtcNow.AddHours(-1),
        ArrivalDate = DateTime.UtcNow.AddHours(3),
        Capacity = 45,
        AvailableSeats = 45,
        Price = 15000m,
        Status = status
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

    private static FinishTripCommand CreateCommand(Guid tripId, string? observations = null) => new()
    {
        Id = tripId,
        ActualArrivalDate = DateTime.UtcNow,
        Observations = observations
    };

    private static FinishTripCommandHandler CreateHandler(
        Trip? trip,
        FakeTripRepository? tripRepository = null,
        FakeBoardingRecordRepository? boardingRecordRepository = null,
        IReadOnlyList<Reservation>? reservations = null,
        IReadOnlyDictionary<Guid, Passenger>? passengersById = null,
        IReadOnlyList<Seat>? seats = null,
        FakeEmailService? emailService = null,
        Guid? callerOrganizationId = null)
    {
        return new FinishTripCommandHandler(
            tripRepository ?? new FakeTripRepository(trip),
            boardingRecordRepository ?? new FakeBoardingRecordRepository(),
            new FakeReservationRepository(reservations ?? []),
            new FakePassengerRepository(passengersById ?? new Dictionary<Guid, Passenger>()),
            new FakeSeatRepository(seats ?? []),
            emailService ?? new FakeEmailService(),
            new FakeCurrentUserService(callerOrganizationId ?? trip?.OrganizationId ?? Guid.NewGuid()),
            NullLogger<FinishTripCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_TripNotFound_ThrowsTripNotFoundException()
    {
        var handler = CreateHandler(trip: null, callerOrganizationId: Guid.NewGuid());

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(CreateCommand(Guid.NewGuid()), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_TripBelongsToAnotherOrganization_ThrowsTripNotFoundException()
    {
        var trip = CreateTrip();
        var handler = CreateHandler(trip, callerOrganizationId: Guid.NewGuid());

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(CreateCommand(trip.Id), TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(TripStatus.Programado)]
    [InlineData(TripStatus.Publicado)]
    [InlineData(TripStatus.Completado)]
    [InlineData(TripStatus.Cancelado)]
    public async Task Handle_TripNotEnProceso_ThrowsTripNotEligibleException(TripStatus status)
    {
        var trip = CreateTrip(status);
        var tripRepository = new FakeTripRepository(trip);
        var boardingRecordRepository = new FakeBoardingRecordRepository();
        var handler = CreateHandler(trip, tripRepository, boardingRecordRepository);

        await Assert.ThrowsAsync<TripNotEligibleException>(() =>
            handler.Handle(CreateCommand(trip.Id), TestContext.Current.CancellationToken));

        Assert.Equal(0, tripRepository.UpdateCallCount);
        Assert.Empty(boardingRecordRepository.ClosedTripIds);
    }

    [Fact]
    public async Task Handle_HappyPath_ClosesBoardingAndTransitionsToCompletado()
    {
        var trip = CreateTrip();
        var tripRepository = new FakeTripRepository(trip);
        var boardingRecordRepository = new FakeBoardingRecordRepository();
        var handler = CreateHandler(trip, tripRepository, boardingRecordRepository);
        var command = CreateCommand(trip.Id, observations: "Llegada con 10 minutos de demora.");

        await handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.Equal([trip.Id], boardingRecordRepository.ClosedTripIds);
        Assert.Equal(1, tripRepository.UpdateCallCount);
        Assert.Equal(TripStatus.Completado, tripRepository.UpdatedTrip?.Status);
        Assert.Equal(command.ActualArrivalDate, tripRepository.UpdatedTrip?.ActualArrivalDate);
        Assert.Equal("Llegada con 10 minutos de demora.", tripRepository.UpdatedTrip?.Observations);
    }

    [Fact]
    public async Task Handle_MultiplePassengers_SendsOneEmailEachWithCorrectSeatNumber()
    {
        var seatId1 = Guid.NewGuid();
        var seatId2 = Guid.NewGuid();
        var trip = CreateTrip();
        var passenger1 = CreatePassenger("ana@checkbus.test");
        var passenger2 = CreatePassenger("beto@checkbus.test");
        var r1 = CreateReservation(trip.Id, seatId1, passenger1.Id);
        var r2 = CreateReservation(trip.Id, seatId2, passenger2.Id);
        var seats = new List<Seat>
        {
            new() { Id = seatId1, VehicleId = trip.VehicleId, Number = 5 },
            new() { Id = seatId2, VehicleId = trip.VehicleId, Number = 12 }
        };
        var passengersById = new Dictionary<Guid, Passenger> { [passenger1.Id] = passenger1, [passenger2.Id] = passenger2 };
        var emailService = new FakeEmailService();

        var handler = CreateHandler(
            trip, reservations: [r1, r2], passengersById: passengersById, seats: seats, emailService: emailService);

        await handler.Handle(CreateCommand(trip.Id), TestContext.Current.CancellationToken);

        Assert.Equal(2, emailService.SentMessages.Count);
        Assert.Contains(emailService.SentMessages, m => m.To == passenger1.Email && m.HtmlBody.Contains("Asiento: 5"));
        Assert.Contains(emailService.SentMessages, m => m.To == passenger2.Email && m.HtmlBody.Contains("Asiento: 12"));
    }

    [Fact]
    public async Task Handle_PassengerHasNoEmail_DoesNotAttemptToSendForThatOne()
    {
        var trip = CreateTrip();
        var passenger = CreatePassenger(email: "");
        var reservation = CreateReservation(trip.Id, Guid.NewGuid(), passenger.Id);
        var passengersById = new Dictionary<Guid, Passenger> { [passenger.Id] = passenger };
        var emailService = new FakeEmailService();

        var handler = CreateHandler(trip, reservations: [reservation], passengersById: passengersById, emailService: emailService);

        await handler.Handle(CreateCommand(trip.Id), TestContext.Current.CancellationToken);

        Assert.Empty(emailService.SentMessages);
    }

    [Fact]
    public async Task Handle_EmailFailsForOnePassenger_TripStillCompletesAndOtherEmailsStillSend()
    {
        var trip = CreateTrip();
        var tripRepository = new FakeTripRepository(trip);
        var passenger = CreatePassenger();
        var reservation = CreateReservation(trip.Id, Guid.NewGuid(), passenger.Id);
        var passengersById = new Dictionary<Guid, Passenger> { [passenger.Id] = passenger };
        var emailService = new FakeEmailService(throwOnSend: true);

        var handler = CreateHandler(
            trip, tripRepository, reservations: [reservation], passengersById: passengersById, emailService: emailService);

        await handler.Handle(CreateCommand(trip.Id), TestContext.Current.CancellationToken);

        Assert.Equal(TripStatus.Completado, tripRepository.UpdatedTrip?.Status);
        Assert.Empty(emailService.SentMessages);
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var trip = CreateTrip();
        var handler = new FinishTripCommandHandler(
            new FakeTripRepository(trip),
            new FakeBoardingRecordRepository(),
            new FakeReservationRepository([]),
            new FakePassengerRepository(new Dictionary<Guid, Passenger>()),
            new FakeSeatRepository([]),
            new FakeEmailService(),
            new FakeCurrentUserService(organizationId: null),
            NullLogger<FinishTripCommandHandler>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(CreateCommand(trip.Id), TestContext.Current.CancellationToken));
    }
}
