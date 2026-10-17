using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Queries;
using Checkbus.ApiService.Domain.Entities.BoardingRecords;
using Checkbus.ApiService.Domain.Entities.Passengers;
using Checkbus.ApiService.Domain.Entities.Reservations;
using Checkbus.ApiService.Domain.Entities.Seats;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Trips;

namespace Checkbus.Tests.Boarding;

/// <summary>
/// Proves <see cref="GetTripBoardingListQueryHandler"/> (odd/tasks/reservas-publicas.md Fase 3 /
/// A3): IDOR-safe ownership check (non-existent, cross-tenant, and someone-else's-trip all throw
/// the same <see cref="TripNotFoundException"/>), ensures BoardingRecord rows before reading them
/// back, and excludes Passenger.Email/Phone from the returned DTO shape. Fake-repository style,
/// matching <see cref="CreatePublicReservationCommandHandlerTests"/>.
/// </summary>
public class GetTripBoardingListQueryHandlerTests
{
    private sealed class FakeTripRepository(Trip? trip) : ITripRepository
    {
        public Task AddAsync(Trip trip, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripBoardingListQueryHandler.");

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(trip);

        public Task UpdateAsync(Trip trip, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripBoardingListQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripBoardingListQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(Guid vehicleId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripBoardingListQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(Guid driverId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripBoardingListQueryHandler.");

        public Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripBoardingListQueryHandler.");

        public Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripBoardingListQueryHandler.");

        public Task UpdateStopDepartureTimesAsync(
            IReadOnlyDictionary<Guid, DateTime> departureTimesByStopId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripBoardingListQueryHandler.");

        public Task<bool> SlugExistsAsync(Guid organizationId, string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripBoardingListQueryHandler.");

        public Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripBoardingListQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetPublicUpcomingByOrganizationIdAsync(
            Guid organizationId, DateTime? dateFrom, DateTime? dateTo, string? eventName, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripBoardingListQueryHandler.");
    }

    private sealed class FakeBoardingRecordRepository(List<BoardingRecord> records) : IBoardingRecordRepository
    {
        public bool EnsureForTripCalled { get; private set; }

        public Task EnsureForTripAsync(Guid tripId, CancellationToken cancellationToken)
        {
            EnsureForTripCalled = true;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<BoardingRecord>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<BoardingRecord>>(records);

        public Task<BoardingRecord?> GetByReservationIdAsync(Guid reservationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripBoardingListQueryHandler.");

        public Task ValidateAsync(Guid reservationId, ValidationMethod method, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripBoardingListQueryHandler.");

        public Task CloseBoardingAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripBoardingListQueryHandler.");
    }

    private sealed class FakeReservationRepository(IReadOnlyList<Reservation> reservations) : IReservationRepository
    {
        public Task<IReadOnlyList<Reservation>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => Task.FromResult(reservations.Where(r => r.TripId == tripId).ToList() as IReadOnlyList<Reservation>);

        public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripBoardingListQueryHandler.");

        public Task CreateBatchAsync(Checkbus.ApiService.Domain.Entities.Payments.Payment payment, IReadOnlyCollection<Passenger> passengers, IReadOnlyCollection<Reservation> reservations, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripBoardingListQueryHandler.");

        public Task CancelAsync(Guid reservationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripBoardingListQueryHandler.");
    }

    private sealed class FakeSeatRepository(IReadOnlyList<Seat> seats) : ISeatRepository
    {
        public Task EnsureSeatsForVehicleAsync(Guid vehicleId, int capacity, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripBoardingListQueryHandler.");

        public Task<IReadOnlyList<Seat>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Seat>>(seats.Where(s => s.VehicleId == vehicleId).ToList());
    }

    private sealed class FakePassengerRepository(IReadOnlyList<Passenger> passengers) : IPassengerRepository
    {
        public Task<Passenger?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(passengers.FirstOrDefault(p => p.Id == id));
    }

    private sealed class FakeCurrentUserService(Guid? organizationId, Guid? userId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => userId;
        public Guid? OrganizationId => organizationId;
        public string? Role => "Chofer";
        public string? Email => "chofer@checkbus-demo.com";
    }

    private static Trip CreateTrip(Guid organizationId, Guid driverId, Guid vehicleId) => new()
    {
        Id = Guid.NewGuid(),
        Slug = Guid.NewGuid().ToString(),
        OrganizationId = organizationId,
        DriverId = driverId,
        VehicleId = vehicleId,
        EventId = Guid.NewGuid(),
        DepartureDate = DateTime.UtcNow,
        ArrivalDate = DateTime.UtcNow.AddHours(8),
        Capacity = 45,
        AvailableSeats = 44,
        Price = 15000m,
        Status = TripStatus.Publicado
    };

    private static Passenger CreatePassenger() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Juan",
        Surname = "Perez",
        DocumentType = DocumentType.DNI,
        DocumentNumber = "30111222",
        Email = "juan@checkbus.test",
        Phone = "+54 11 5555-5555"
    };

    private GetTripBoardingListQueryHandler CreateHandler(
        Trip? trip,
        List<BoardingRecord> boardingRecords,
        IReadOnlyList<Reservation> reservations,
        IReadOnlyList<Seat> seats,
        IReadOnlyList<Passenger> passengers,
        Guid? organizationId,
        Guid? userId,
        out FakeBoardingRecordRepository boardingRecordRepository)
    {
        boardingRecordRepository = new FakeBoardingRecordRepository(boardingRecords);
        return new GetTripBoardingListQueryHandler(
            new FakeTripRepository(trip),
            boardingRecordRepository,
            new FakeReservationRepository(reservations),
            new FakeSeatRepository(seats),
            new FakePassengerRepository(passengers),
            new FakeCurrentUserService(organizationId, userId));
    }

    [Fact]
    public async Task Handle_HappyPath_EnsuresRecordsAndReturnsListExcludingContactData()
    {
        var organizationId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var trip = CreateTrip(organizationId, driverId, vehicleId);

        var passenger = CreatePassenger();
        var seat = new Seat { Id = Guid.NewGuid(), VehicleId = vehicleId, Number = 7 };
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            SeatId = seat.Id,
            PassengerId = passenger.Id,
            Price = trip.Price
        };
        var boardingRecord = new BoardingRecord { Id = Guid.NewGuid(), ReservationId = reservation.Id, Status = BoardingStatus.Abordado };

        var handler = CreateHandler(
            trip, [boardingRecord], [reservation], [seat], [passenger], organizationId, driverId,
            out var boardingRecordRepository);

        var result = await handler.Handle(
            new GetTripBoardingListQuery { TripId = trip.Id }, TestContext.Current.CancellationToken);

        Assert.True(boardingRecordRepository.EnsureForTripCalled);
        var dto = Assert.Single(result.Passengers);
        Assert.Equal(reservation.Id, dto.ReservationId);
        Assert.Equal(7, dto.SeatNumber);
        Assert.Equal(passenger.Name, dto.PassengerName);
        Assert.Equal(passenger.Surname, dto.PassengerSurname);
        Assert.Equal(passenger.DocumentNumber, dto.PassengerDocumentNumber);
        Assert.Equal(BoardingStatus.Abordado, dto.Status);

        // Deliberately excluded from the DTO shape — not just unset.
        Assert.DoesNotContain(
            typeof(BoardingPassengerDto).GetProperties(), p => p.Name is "PassengerEmail" or "PassengerPhone");
    }

    [Fact]
    public async Task Handle_NoBoardingRecordYet_DefaultsToPendiente()
    {
        var organizationId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var trip = CreateTrip(organizationId, driverId, vehicleId);

        var passenger = CreatePassenger();
        var seat = new Seat { Id = Guid.NewGuid(), VehicleId = vehicleId, Number = 1 };
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            SeatId = seat.Id,
            PassengerId = passenger.Id,
            Price = trip.Price
        };

        var handler = CreateHandler(
            trip, [], [reservation], [seat], [passenger], organizationId, driverId, out _);

        var result = await handler.Handle(
            new GetTripBoardingListQuery { TripId = trip.Id }, TestContext.Current.CancellationToken);

        Assert.Equal(BoardingStatus.Pendiente, Assert.Single(result.Passengers).Status);
    }

    [Fact]
    public async Task Handle_TripNotFound_ThrowsTripNotFound()
    {
        var handler = CreateHandler(null, [], [], [], [], Guid.NewGuid(), Guid.NewGuid(), out _);

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(new GetTripBoardingListQuery { TripId = Guid.NewGuid() }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_TripBelongsToDifferentOrganization_ThrowsTripNotFound()
    {
        var driverId = Guid.NewGuid();
        var trip = CreateTrip(Guid.NewGuid(), driverId, Guid.NewGuid());

        var handler = CreateHandler(trip, [], [], [], [], Guid.NewGuid(), driverId, out _);

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(new GetTripBoardingListQuery { TripId = trip.Id }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_TripBelongsToDifferentDriver_ThrowsTripNotFound()
    {
        var organizationId = Guid.NewGuid();
        var trip = CreateTrip(organizationId, Guid.NewGuid(), Guid.NewGuid());

        var handler = CreateHandler(trip, [], [], [], [], organizationId, Guid.NewGuid(), out _);

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(new GetTripBoardingListQuery { TripId = trip.Id }, TestContext.Current.CancellationToken));
    }
}
