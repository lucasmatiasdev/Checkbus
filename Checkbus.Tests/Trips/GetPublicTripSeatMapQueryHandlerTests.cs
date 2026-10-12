using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Queries;
using Checkbus.ApiService.Domain.Entities.Reservations;
using Checkbus.ApiService.Domain.Entities.Seats;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Trips;

namespace Checkbus.Tests.Trips;

/// <summary>
/// Proves <see cref="GetPublicTripSeatMapQueryHandler"/> — the project's first
/// business-domain anonymous surface (odd/tasks/reservas-publicas.md R3) — gates purely on
/// <c>Trip.Status == Publicado</c>, with no <c>ICurrentUserService</c> and no org scoping.
/// Fake-repository style, matching <see cref="PublishTripCommandHandlerTests"/> and
/// <see cref="GetTripQueryHandlerTests"/>.
/// </summary>
public class GetPublicTripSeatMapQueryHandlerTests
{
    private sealed class FakeTripRepository(Trip? trip) : ITripRepository
    {
        public Task AddAsync(Trip trip, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripSeatMapQueryHandler.");

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(trip);

        public Task UpdateAsync(Trip trip, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripSeatMapQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripSeatMapQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(Guid vehicleId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripSeatMapQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(Guid driverId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripSeatMapQueryHandler.");

        public Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripSeatMapQueryHandler.");

        public Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripSeatMapQueryHandler.");

        public Task UpdateStopDepartureTimesAsync(
            IReadOnlyDictionary<Guid, DateTime> departureTimesByStopId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripSeatMapQueryHandler.");
    }

    private sealed class FakeVehicleRepository(Vehicle? vehicle) : IVehicleRepository
    {
        public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(vehicle);

        public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripSeatMapQueryHandler.");

        public Task<IReadOnlyList<Vehicle>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripSeatMapQueryHandler.");

        public Task<bool> PatentExistsAsync(string patent, Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripSeatMapQueryHandler.");
    }

    private sealed class FakeSeatRepository(List<Seat> seats) : ISeatRepository
    {
        public bool EnsureCalled { get; private set; }
        public int? EnsureCalledWithCapacity { get; private set; }

        public Task EnsureSeatsForVehicleAsync(Guid vehicleId, int capacity, CancellationToken cancellationToken)
        {
            EnsureCalled = true;
            EnsureCalledWithCapacity = capacity;

            var existingNumbers = seats.Where(s => s.VehicleId == vehicleId).Select(s => s.Number).ToHashSet();
            foreach (var number in Enumerable.Range(1, capacity).Where(n => !existingNumbers.Contains(n)))
            {
                seats.Add(new Seat { Id = Guid.NewGuid(), VehicleId = vehicleId, Number = number });
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Seat>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Seat>>(seats.Where(s => s.VehicleId == vehicleId).ToList());
    }

    private sealed class FakeReservationRepository(IReadOnlyList<Reservation> reservations) : IReservationRepository
    {
        public bool ReleaseExpiredHoldsCalled { get; private set; }

        public Task<IReadOnlyList<Reservation>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            // Excludes Cancelada, matching ReservationRepository's real contract — needed so a
            // hold just released by ReleaseExpiredHoldsAsync below correctly stops counting as
            // occupied in the same Handle() call.
            => Task.FromResult<IReadOnlyList<Reservation>>(
                reservations.Where(r => r.TripId == tripId && r.Status != ReservationStatus.Cancelada).ToList());

        public Task CreateBatchAsync(
            Checkbus.ApiService.Domain.Entities.Payments.Payment payment,
            IReadOnlyCollection<Checkbus.ApiService.Domain.Entities.Passengers.Passenger> passengers,
            IReadOnlyCollection<Reservation> newReservations,
            CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripSeatMapQueryHandler.");

        public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripSeatMapQueryHandler.");

        public Task CancelAsync(Guid reservationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripSeatMapQueryHandler.");

        // Fase E: mirrors ReservationRepository.ReleaseExpiredHoldsAsync's real behavior
        // in-memory, so this handler's own tests can prove the call site actually releases an
        // expired hold before occupancy is computed, not just that it was invoked.
        public Task ReleaseExpiredHoldsAsync(Guid tripId, int holdExpirationMinutes, CancellationToken cancellationToken)
        {
            ReleaseExpiredHoldsCalled = true;
            var cutoff = DateTime.UtcNow.AddMinutes(-holdExpirationMinutes);
            foreach (var reservation in reservations.Where(r =>
                r.TripId == tripId && r.Status == ReservationStatus.PendientePago && r.CreatedAt < cutoff))
            {
                reservation.Status = ReservationStatus.Cancelada;
            }

            return Task.CompletedTask;
        }
    }

    private static Trip CreateTrip(TripStatus status = TripStatus.Publicado) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = Guid.NewGuid(),
        DriverId = Guid.NewGuid(),
        VehicleId = Guid.NewGuid(),
        EventId = Guid.NewGuid(),
        DepartureDate = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc),
        ArrivalDate = new DateTime(2026, 5, 1, 18, 0, 0, DateTimeKind.Utc),
        Capacity = 45,
        AvailableSeats = 45,
        Price = 15000m,
        Status = status
    };

    private static Vehicle CreateVehicle(Guid id, int capacity) => new()
    {
        Id = id,
        Brand = "Mercedes-Benz",
        Model = "O500",
        Year = 2020,
        Patent = "AB123CD",
        Capacity = capacity,
        Mileage = 1000,
        Status = VehicleStatus.Activo,
        OrganizationId = Guid.NewGuid(),
        OwnerType = VehicleOwnerType.Organizacion
    };

    [Fact]
    public async Task Handle_PublishedTrip_ReturnsCapacityPriceAndOccupiedSeatNumbers()
    {
        var trip = CreateTrip();
        var vehicle = CreateVehicle(trip.VehicleId, capacity: 4);
        var seats = new List<Seat>
        {
            new() { Id = Guid.NewGuid(), VehicleId = trip.VehicleId, Number = 1 },
            new() { Id = Guid.NewGuid(), VehicleId = trip.VehicleId, Number = 2 },
            new() { Id = Guid.NewGuid(), VehicleId = trip.VehicleId, Number = 3 },
            new() { Id = Guid.NewGuid(), VehicleId = trip.VehicleId, Number = 4 }
        };
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            SeatId = seats[1].Id, // Number = 2
            PassengerId = Guid.NewGuid(),
            Price = trip.Price
        };

        var handler = new GetPublicTripSeatMapQueryHandler(
            new FakeTripRepository(trip),
            new FakeVehicleRepository(vehicle),
            new FakeSeatRepository(seats),
            new FakeReservationRepository([reservation]));

        var result = await handler.Handle(
            new GetPublicTripSeatMapQuery { TripId = trip.Id }, TestContext.Current.CancellationToken);

        Assert.Equal(trip.Id, result.TripId);
        Assert.Equal(4, result.Capacity);
        Assert.Equal(trip.Price, result.Price);
        Assert.Equal([2], result.OccupiedSeatNumbers);
        Assert.Equal(4, result.Seats.Count);
        Assert.Equal(seats[1].Id, result.Seats.Single(s => s.Number == 2).SeatId);
    }

    [Fact]
    public async Task Handle_TripNotFound_ThrowsTripNotFound()
    {
        var handler = new GetPublicTripSeatMapQueryHandler(
            new FakeTripRepository(null),
            new FakeVehicleRepository(null),
            new FakeSeatRepository([]),
            new FakeReservationRepository([]));

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(new GetPublicTripSeatMapQuery { TripId = Guid.NewGuid() }, TestContext.Current.CancellationToken));
    }

    // Fase E (odd/tasks/pagos-mercadopago.md): a PendientePago hold older than
    // HoldExpirationMinutes must be released (and its seat freed) on the very next seat-map
    // read — proves the call site wires ReleaseExpiredHoldsAsync in before occupancy is
    // computed, not just that the repository method itself works.
    [Fact]
    public async Task Handle_PendientePagoReservationOlderThanHoldExpiration_IsReleasedAndSeatBecomesAvailable()
    {
        var trip = CreateTrip();
        var vehicle = CreateVehicle(trip.VehicleId, capacity: 2);
        var seats = new List<Seat>
        {
            new() { Id = Guid.NewGuid(), VehicleId = trip.VehicleId, Number = 1 },
            new() { Id = Guid.NewGuid(), VehicleId = trip.VehicleId, Number = 2 }
        };
        var expiredHold = new Reservation
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            SeatId = seats[0].Id,
            PassengerId = Guid.NewGuid(),
            Price = trip.Price,
            Status = ReservationStatus.PendientePago,
            CreatedAt = DateTime.UtcNow.AddMinutes(-20)
        };
        var reservationRepository = new FakeReservationRepository([expiredHold]);

        var handler = new GetPublicTripSeatMapQueryHandler(
            new FakeTripRepository(trip),
            new FakeVehicleRepository(vehicle),
            new FakeSeatRepository(seats),
            reservationRepository);

        var result = await handler.Handle(
            new GetPublicTripSeatMapQuery { TripId = trip.Id }, TestContext.Current.CancellationToken);

        Assert.True(reservationRepository.ReleaseExpiredHoldsCalled);
        Assert.Equal(ReservationStatus.Cancelada, expiredHold.Status);
        Assert.Empty(result.OccupiedSeatNumbers);
    }

    // The mirror case: a PendientePago hold still inside the window must survive untouched —
    // the seat stays occupied.
    [Fact]
    public async Task Handle_PendientePagoReservationYoungerThanHoldExpiration_IsLeftUntouched()
    {
        var trip = CreateTrip();
        var vehicle = CreateVehicle(trip.VehicleId, capacity: 2);
        var seats = new List<Seat>
        {
            new() { Id = Guid.NewGuid(), VehicleId = trip.VehicleId, Number = 1 },
            new() { Id = Guid.NewGuid(), VehicleId = trip.VehicleId, Number = 2 }
        };
        var freshHold = new Reservation
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            SeatId = seats[0].Id,
            PassengerId = Guid.NewGuid(),
            Price = trip.Price,
            Status = ReservationStatus.PendientePago,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        var handler = new GetPublicTripSeatMapQueryHandler(
            new FakeTripRepository(trip),
            new FakeVehicleRepository(vehicle),
            new FakeSeatRepository(seats),
            new FakeReservationRepository([freshHold]));

        var result = await handler.Handle(
            new GetPublicTripSeatMapQuery { TripId = trip.Id }, TestContext.Current.CancellationToken);

        Assert.Equal(ReservationStatus.PendientePago, freshHold.Status);
        Assert.Equal([1], result.OccupiedSeatNumbers);
    }

    [Theory]
    [InlineData(TripStatus.Programado)]
    [InlineData(TripStatus.EnProceso)]
    [InlineData(TripStatus.Completado)]
    [InlineData(TripStatus.Cancelado)]
    public async Task Handle_TripExistsButNotPublicado_ThrowsSameTripNotFound(TripStatus status)
    {
        // Indistinguishable from "truly doesn't exist" at the exception-type level — the
        // whole point of this public surface's IDOR-safe gate.
        var trip = CreateTrip(status);
        var handler = new GetPublicTripSeatMapQueryHandler(
            new FakeTripRepository(trip),
            new FakeVehicleRepository(CreateVehicle(trip.VehicleId, 45)),
            new FakeSeatRepository([]),
            new FakeReservationRepository([]));

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(new GetPublicTripSeatMapQuery { TripId = trip.Id }, TestContext.Current.CancellationToken));
    }
}
