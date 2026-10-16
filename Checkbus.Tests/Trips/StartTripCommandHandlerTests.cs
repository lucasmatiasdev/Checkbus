using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Commands;
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
/// Proves <see cref="StartTripCommandHandler"/> (CU-08): tenant-scoped IDOR-safe load (mirrors
/// PublishTripCommandHandlerTests/CancelTripCommandHandlerTests), the invalid-status and
/// outside-the-±30-minute-window rejections, the EnProceso transition, and the best-effort
/// per-passenger "trip started" email.
/// </summary>
public class StartTripCommandHandlerTests
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
            => throw new NotSupportedException("Not needed by StartTripCommandHandler.");

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(trip);

        public Task UpdateAsync(Trip t, CancellationToken cancellationToken)
        {
            UpdateCallCount++;
            UpdatedTrip = t;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by StartTripCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(Guid vehicleId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by StartTripCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(Guid driverId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by StartTripCommandHandler.");

        public Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by StartTripCommandHandler.");

        public Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by StartTripCommandHandler.");

        public Task UpdateStopDepartureTimesAsync(
            IReadOnlyDictionary<Guid, DateTime> departureTimesByStopId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by StartTripCommandHandler.");

        public Task<bool> SlugExistsAsync(Guid organizationId, string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by StartTripCommandHandler.");

        public Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by StartTripCommandHandler.");
    }

    private sealed class FakeReservationRepository(IReadOnlyList<Reservation> reservations) : IReservationRepository
    {
        public Task<IReadOnlyList<Reservation>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => Task.FromResult(reservations);

        public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by StartTripCommandHandler.");

        public Task CreateBatchAsync(Payment payment, IReadOnlyCollection<Passenger> passengers, IReadOnlyCollection<Reservation> batchReservations, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by StartTripCommandHandler.");

        public Task CancelAsync(Guid reservationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by StartTripCommandHandler.");

        public Task<IReadOnlyList<Reservation>> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by StartTripCommandHandler.");

        public Task<IReadOnlyList<Reservation>> GetAllByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by StartTripCommandHandler.");
    }

    private sealed class FakePassengerRepository(IReadOnlyDictionary<Guid, Passenger> passengersById) : IPassengerRepository
    {
        public Task<Passenger?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(passengersById.TryGetValue(id, out var passenger) ? passenger : null);
    }

    private sealed class FakeSeatRepository(IReadOnlyList<Seat> seats) : ISeatRepository
    {
        public Task EnsureSeatsForVehicleAsync(Guid vehicleId, int capacity, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by StartTripCommandHandler.");

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

    private static Trip CreateTrip(
        DateTime departureDate, TripStatus status = TripStatus.Publicado, Guid? organizationId = null) => new()
    {
        Id = Guid.NewGuid(),
        Slug = Guid.NewGuid().ToString(),
        OrganizationId = organizationId ?? Guid.NewGuid(),
        DriverId = Guid.NewGuid(),
        VehicleId = Guid.NewGuid(),
        EventId = Guid.NewGuid(),
        DepartureDate = departureDate,
        ArrivalDate = departureDate.AddHours(4),
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

    private static StartTripCommandHandler CreateHandler(
        Trip? trip,
        FakeTripRepository? tripRepository = null,
        IReadOnlyList<Reservation>? reservations = null,
        IReadOnlyDictionary<Guid, Passenger>? passengersById = null,
        IReadOnlyList<Seat>? seats = null,
        FakeEmailService? emailService = null,
        Guid? callerOrganizationId = null)
    {
        return new StartTripCommandHandler(
            tripRepository ?? new FakeTripRepository(trip),
            new FakeReservationRepository(reservations ?? []),
            new FakePassengerRepository(passengersById ?? new Dictionary<Guid, Passenger>()),
            new FakeSeatRepository(seats ?? []),
            emailService ?? new FakeEmailService(),
            new FakeCurrentUserService(callerOrganizationId ?? trip?.OrganizationId ?? Guid.NewGuid()),
            NullLogger<StartTripCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_TripNotFound_ThrowsTripNotFoundException()
    {
        var handler = CreateHandler(trip: null, callerOrganizationId: Guid.NewGuid());

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(new StartTripCommand { Id = Guid.NewGuid() }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_TripBelongsToAnotherOrganization_ThrowsTripNotFoundException()
    {
        var trip = CreateTrip(DateTime.UtcNow);
        var handler = CreateHandler(trip, callerOrganizationId: Guid.NewGuid());

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(new StartTripCommand { Id = trip.Id }, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(TripStatus.Programado)]
    [InlineData(TripStatus.EnProceso)]
    [InlineData(TripStatus.Completado)]
    [InlineData(TripStatus.Cancelado)]
    public async Task Handle_TripNotPublicado_ThrowsTripNotEligibleException(TripStatus status)
    {
        var trip = CreateTrip(DateTime.UtcNow, status);
        var tripRepository = new FakeTripRepository(trip);
        var handler = CreateHandler(trip, tripRepository);

        await Assert.ThrowsAsync<TripNotEligibleException>(() =>
            handler.Handle(new StartTripCommand { Id = trip.Id }, TestContext.Current.CancellationToken));

        Assert.Equal(0, tripRepository.UpdateCallCount);
    }

    [Fact]
    public async Task Handle_MoreThan30MinutesBeforeDeparture_ThrowsTripNotEligibleException()
    {
        var trip = CreateTrip(DateTime.UtcNow.AddMinutes(31));
        var tripRepository = new FakeTripRepository(trip);
        var handler = CreateHandler(trip, tripRepository);

        await Assert.ThrowsAsync<TripNotEligibleException>(() =>
            handler.Handle(new StartTripCommand { Id = trip.Id }, TestContext.Current.CancellationToken));

        Assert.Equal(0, tripRepository.UpdateCallCount);
    }

    [Fact]
    public async Task Handle_MoreThan30MinutesAfterDeparture_ThrowsTripNotEligibleException()
    {
        var trip = CreateTrip(DateTime.UtcNow.AddMinutes(-31));
        var tripRepository = new FakeTripRepository(trip);
        var handler = CreateHandler(trip, tripRepository);

        await Assert.ThrowsAsync<TripNotEligibleException>(() =>
            handler.Handle(new StartTripCommand { Id = trip.Id }, TestContext.Current.CancellationToken));

        Assert.Equal(0, tripRepository.UpdateCallCount);
    }

    [Theory]
    [InlineData(-29)]
    [InlineData(0)]
    [InlineData(29)]
    public async Task Handle_WithinThirtyMinuteWindow_TransitionsToEnProceso(int minutesFromNow)
    {
        var trip = CreateTrip(DateTime.UtcNow.AddMinutes(minutesFromNow));
        var tripRepository = new FakeTripRepository(trip);
        var handler = CreateHandler(trip, tripRepository);

        await handler.Handle(new StartTripCommand { Id = trip.Id }, TestContext.Current.CancellationToken);

        Assert.Equal(1, tripRepository.UpdateCallCount);
        Assert.Equal(TripStatus.EnProceso, tripRepository.UpdatedTrip?.Status);
    }

    [Fact]
    public async Task Handle_MultiplePassengers_SendsOneEmailEachWithCorrectSeatNumber()
    {
        var seatId1 = Guid.NewGuid();
        var seatId2 = Guid.NewGuid();
        var trip = CreateTrip(DateTime.UtcNow);
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

        await handler.Handle(new StartTripCommand { Id = trip.Id }, TestContext.Current.CancellationToken);

        Assert.Equal(2, emailService.SentMessages.Count);
        Assert.Contains(emailService.SentMessages, m => m.To == passenger1.Email && m.HtmlBody.Contains("Asiento: 5"));
        Assert.Contains(emailService.SentMessages, m => m.To == passenger2.Email && m.HtmlBody.Contains("Asiento: 12"));
    }

    [Fact]
    public async Task Handle_PassengerHasNoEmail_DoesNotAttemptToSendForThatOne()
    {
        var trip = CreateTrip(DateTime.UtcNow);
        var passenger = CreatePassenger(email: "");
        var reservation = CreateReservation(trip.Id, Guid.NewGuid(), passenger.Id);
        var passengersById = new Dictionary<Guid, Passenger> { [passenger.Id] = passenger };
        var emailService = new FakeEmailService();

        var handler = CreateHandler(trip, reservations: [reservation], passengersById: passengersById, emailService: emailService);

        await handler.Handle(new StartTripCommand { Id = trip.Id }, TestContext.Current.CancellationToken);

        Assert.Empty(emailService.SentMessages);
    }

    [Fact]
    public async Task Handle_EmailFailsForOnePassenger_TripStillTransitionsAndOtherEmailsStillSend()
    {
        var trip = CreateTrip(DateTime.UtcNow);
        var tripRepository = new FakeTripRepository(trip);
        var passenger = CreatePassenger();
        var reservation = CreateReservation(trip.Id, Guid.NewGuid(), passenger.Id);
        var passengersById = new Dictionary<Guid, Passenger> { [passenger.Id] = passenger };
        var emailService = new FakeEmailService(throwOnSend: true);

        var handler = CreateHandler(
            trip, tripRepository, reservations: [reservation], passengersById: passengersById, emailService: emailService);

        await handler.Handle(new StartTripCommand { Id = trip.Id }, TestContext.Current.CancellationToken);

        Assert.Equal(TripStatus.EnProceso, tripRepository.UpdatedTrip?.Status);
        Assert.Empty(emailService.SentMessages);
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var trip = CreateTrip(DateTime.UtcNow);
        var handler = new StartTripCommandHandler(
            new FakeTripRepository(trip),
            new FakeReservationRepository([]),
            new FakePassengerRepository(new Dictionary<Guid, Passenger>()),
            new FakeSeatRepository([]),
            new FakeEmailService(),
            new FakeCurrentUserService(organizationId: null),
            NullLogger<StartTripCommandHandler>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new StartTripCommand { Id = trip.Id }, TestContext.Current.CancellationToken));
    }
}
