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
        public Task<IReadOnlyList<Reservation>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Reservation>>(reservations.Where(r => r.TripId == tripId).ToList());

        public Task CreateBatchAsync(
            IReadOnlyCollection<Checkbus.ApiService.Domain.Entities.Passengers.Passenger> passengers,
            IReadOnlyCollection<Reservation> newReservations,
            CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripSeatMapQueryHandler.");

        public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripSeatMapQueryHandler.");

        public Task CancelAsync(Guid reservationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicTripSeatMapQueryHandler.");
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
