using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Commands;
using Checkbus.ApiService.Domain.Entities.BoardingRecords;
using Checkbus.ApiService.Domain.Entities.Passengers;
using Checkbus.ApiService.Domain.Entities.Reservations;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Boarding;
using Checkbus.ApiService.Domain.Exceptions.Trips;

namespace Checkbus.Tests.Boarding;

/// <summary>
/// Proves <see cref="ValidateBoardingCommandHandler"/> (odd/tasks/reservas-publicas.md Fase 3 /
/// A4): resolves the owning Trip from ReservationId, applies the same IDOR-safe ownership check
/// as <see cref="GetTripBoardingListQueryHandlerTests"/>, and delegates the actual
/// Pendiente -> Abordado transition (including the already-validated rejection) to
/// <see cref="IBoardingRecordRepository.ValidateAsync"/>. Fake-repository style.
/// </summary>
public class ValidateBoardingCommandHandlerTests
{
    private sealed class FakeReservationRepository(Reservation? reservation) : IReservationRepository
    {
        public Task<IReadOnlyList<Reservation>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ValidateBoardingCommandHandler.");

        public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(reservation);

        public Task CreateBatchAsync(Checkbus.ApiService.Domain.Entities.Payments.Payment payment, IReadOnlyCollection<Passenger> passengers, IReadOnlyCollection<Reservation> reservations, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ValidateBoardingCommandHandler.");

        public Task CancelAsync(Guid reservationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ValidateBoardingCommandHandler.");
    }

    private sealed class FakeTripRepository(Trip? trip) : ITripRepository
    {
        public Task AddAsync(Trip trip, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ValidateBoardingCommandHandler.");

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(trip);

        public Task UpdateAsync(Trip trip, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ValidateBoardingCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ValidateBoardingCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(Guid vehicleId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ValidateBoardingCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(Guid driverId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ValidateBoardingCommandHandler.");

        public Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ValidateBoardingCommandHandler.");

        public Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ValidateBoardingCommandHandler.");

        public Task UpdateStopDepartureTimesAsync(
            IReadOnlyDictionary<Guid, DateTime> departureTimesByStopId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ValidateBoardingCommandHandler.");

        public Task<bool> SlugExistsAsync(Guid organizationId, string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ValidateBoardingCommandHandler.");

        public Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ValidateBoardingCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetPublicUpcomingByOrganizationIdAsync(
            Guid organizationId, DateTime? dateFrom, DateTime? dateTo, string? eventName, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ValidateBoardingCommandHandler.");
    }

    private sealed class FakeBoardingRecordRepository(bool throwAlreadyValidated = false) : IBoardingRecordRepository
    {
        public Guid? ValidatedReservationId { get; private set; }
        public ValidationMethod? ValidatedMethod { get; private set; }

        public Task EnsureForTripAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ValidateBoardingCommandHandler.");

        public Task<IReadOnlyList<BoardingRecord>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ValidateBoardingCommandHandler.");

        public Task<BoardingRecord?> GetByReservationIdAsync(Guid reservationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ValidateBoardingCommandHandler.");

        public Task ValidateAsync(Guid reservationId, ValidationMethod method, CancellationToken cancellationToken)
        {
            if (throwAlreadyValidated)
            {
                throw new BoardingAlreadyValidatedException();
            }

            ValidatedReservationId = reservationId;
            ValidatedMethod = method;
            return Task.CompletedTask;
        }

        public Task CloseBoardingAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ValidateBoardingCommandHandler.");
    }

    private sealed class FakeCurrentUserService(Guid? organizationId, Guid? userId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => userId;
        public Guid? OrganizationId => organizationId;
        public string? Role => "Chofer";
        public string? Email => "chofer@checkbus-demo.com";
    }

    private static Trip CreateTrip(Guid organizationId, Guid driverId) => new()
    {
        Id = Guid.NewGuid(),
        Slug = Guid.NewGuid().ToString(),
        OrganizationId = organizationId,
        DriverId = driverId,
        VehicleId = Guid.NewGuid(),
        EventId = Guid.NewGuid(),
        DepartureDate = DateTime.UtcNow,
        ArrivalDate = DateTime.UtcNow.AddHours(8),
        Capacity = 45,
        AvailableSeats = 44,
        Price = 15000m,
        Status = TripStatus.Publicado
    };

    [Fact]
    public async Task Handle_HappyPath_DelegatesToValidateAsyncWithSameMethod()
    {
        var organizationId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var trip = CreateTrip(organizationId, driverId);
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            SeatId = Guid.NewGuid(),
            PassengerId = Guid.NewGuid(),
            Price = trip.Price
        };
        var boardingRecordRepository = new FakeBoardingRecordRepository();

        var handler = new ValidateBoardingCommandHandler(
            new FakeReservationRepository(reservation),
            new FakeTripRepository(trip),
            boardingRecordRepository,
            new FakeCurrentUserService(organizationId, driverId));

        await handler.Handle(
            new ValidateBoardingCommand { ReservationId = reservation.Id, Method = ValidationMethod.Codigo },
            TestContext.Current.CancellationToken);

        Assert.Equal(reservation.Id, boardingRecordRepository.ValidatedReservationId);
        Assert.Equal(ValidationMethod.Codigo, boardingRecordRepository.ValidatedMethod);
    }

    [Fact]
    public async Task Handle_ReservationNotFound_ThrowsBoardingRecordNotFound()
    {
        var handler = new ValidateBoardingCommandHandler(
            new FakeReservationRepository(null),
            new FakeTripRepository(null),
            new FakeBoardingRecordRepository(),
            new FakeCurrentUserService(Guid.NewGuid(), Guid.NewGuid()));

        await Assert.ThrowsAsync<BoardingRecordNotFoundException>(() =>
            handler.Handle(
                new ValidateBoardingCommand { ReservationId = Guid.NewGuid(), Method = ValidationMethod.Codigo },
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ReservationBelongsToTripOfDifferentDriver_ThrowsTripNotFound()
    {
        var organizationId = Guid.NewGuid();
        var trip = CreateTrip(organizationId, Guid.NewGuid());
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            SeatId = Guid.NewGuid(),
            PassengerId = Guid.NewGuid(),
            Price = trip.Price
        };

        var handler = new ValidateBoardingCommandHandler(
            new FakeReservationRepository(reservation),
            new FakeTripRepository(trip),
            new FakeBoardingRecordRepository(),
            new FakeCurrentUserService(organizationId, Guid.NewGuid()));

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(
                new ValidateBoardingCommand { ReservationId = reservation.Id, Method = ValidationMethod.Codigo },
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_AlreadyValidated_PropagatesBoardingAlreadyValidated()
    {
        var organizationId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var trip = CreateTrip(organizationId, driverId);
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            SeatId = Guid.NewGuid(),
            PassengerId = Guid.NewGuid(),
            Price = trip.Price
        };

        var handler = new ValidateBoardingCommandHandler(
            new FakeReservationRepository(reservation),
            new FakeTripRepository(trip),
            new FakeBoardingRecordRepository(throwAlreadyValidated: true),
            new FakeCurrentUserService(organizationId, driverId));

        await Assert.ThrowsAsync<BoardingAlreadyValidatedException>(() =>
            handler.Handle(
                new ValidateBoardingCommand { ReservationId = reservation.Id, Method = ValidationMethod.Busqueda },
                TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(ReservationStatus.Cancelada)]
    [InlineData(ReservationStatus.PendientePago)]
    public async Task Handle_ReservationNotConfirmada_ThrowsReservationNotEligibleForBoarding(ReservationStatus status)
    {
        var organizationId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var trip = CreateTrip(organizationId, driverId);
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            SeatId = Guid.NewGuid(),
            PassengerId = Guid.NewGuid(),
            Price = trip.Price,
            Status = status
        };
        var boardingRecordRepository = new FakeBoardingRecordRepository();

        var handler = new ValidateBoardingCommandHandler(
            new FakeReservationRepository(reservation),
            new FakeTripRepository(trip),
            boardingRecordRepository,
            new FakeCurrentUserService(organizationId, driverId));

        await Assert.ThrowsAsync<ReservationNotEligibleForBoardingException>(() =>
            handler.Handle(
                new ValidateBoardingCommand { ReservationId = reservation.Id, Method = ValidationMethod.Codigo },
                TestContext.Current.CancellationToken));

        Assert.Null(boardingRecordRepository.ValidatedReservationId);
    }
}
