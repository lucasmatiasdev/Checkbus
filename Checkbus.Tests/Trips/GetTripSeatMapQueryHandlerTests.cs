using Checkbus.ApiService.Application.Interfaces.Authentication;
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
/// Proves <see cref="GetTripSeatMapQueryHandler"/> (odd/tasks/mejoras-detalle-viaje.md Fase
/// E1) — the authenticated, org-scoped counterpart to
/// <see cref="GetPublicTripSeatMapQueryHandler"/>. Mirrors
/// <see cref="GetPublicTripSeatMapQueryHandlerTests"/>'s cases minus the Publicado-gate ones,
/// gated instead by org ownership (same IDOR-safe posture as <see cref="GetTripQueryHandler"/>).
/// </summary>
public class GetTripSeatMapQueryHandlerTests
{
    private sealed class FakeTripRepository(Trip? trip) : ITripRepository
    {
        public Task AddAsync(Trip trip, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripSeatMapQueryHandler.");

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(trip);

        public Task UpdateAsync(Trip trip, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripSeatMapQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripSeatMapQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(Guid vehicleId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripSeatMapQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(Guid driverId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripSeatMapQueryHandler.");

        public Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripSeatMapQueryHandler.");

        public Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripSeatMapQueryHandler.");

        public Task UpdateStopDepartureTimesAsync(
            IReadOnlyDictionary<Guid, DateTime> departureTimesByStopId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripSeatMapQueryHandler.");
    }

    private sealed class FakeVehicleRepository(Vehicle? vehicle) : IVehicleRepository
    {
        public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(vehicle);

        public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripSeatMapQueryHandler.");

        public Task<IReadOnlyList<Vehicle>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripSeatMapQueryHandler.");

        public Task<bool> PatentExistsAsync(string patent, Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripSeatMapQueryHandler.");
    }

    private sealed class FakeSeatRepository(List<Seat> seats) : ISeatRepository
    {
        public Task EnsureSeatsForVehicleAsync(Guid vehicleId, int capacity, CancellationToken cancellationToken)
        {
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
            => throw new NotSupportedException("Not needed by GetTripSeatMapQueryHandler.");

        public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripSeatMapQueryHandler.");

        public Task CancelAsync(Guid reservationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripSeatMapQueryHandler.");
    }

    private sealed class FakeCurrentUserService(Guid? organizationId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.NewGuid();
        public Guid? OrganizationId => organizationId;
        public string? Role => "Planificador";
        public string? Email => "planificador@checkbus-demo.com";
    }

    private static Trip CreateTrip(Guid organizationId, TripStatus status = TripStatus.Programado) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = organizationId,
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

    private static Vehicle CreateVehicle(Guid id, Guid organizationId, int capacity) => new()
    {
        Id = id,
        Brand = "Mercedes-Benz",
        Model = "O500",
        Year = 2020,
        Patent = "AB123CD",
        Capacity = capacity,
        Mileage = 1000,
        Status = VehicleStatus.Activo,
        OrganizationId = organizationId,
        OwnerType = VehicleOwnerType.Organizacion
    };

    [Fact]
    public async Task Handle_TripNotFound_ThrowsTripNotFound()
    {
        var handler = new GetTripSeatMapQueryHandler(
            new FakeTripRepository(null),
            new FakeVehicleRepository(null),
            new FakeSeatRepository([]),
            new FakeReservationRepository([]),
            new FakeCurrentUserService(Guid.NewGuid()));

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(new GetTripSeatMapQuery { TripId = Guid.NewGuid() }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_TripBelongsToDifferentOrganization_ThrowsTripNotFound()
    {
        var trip = CreateTrip(Guid.NewGuid());
        var handler = new GetTripSeatMapQueryHandler(
            new FakeTripRepository(trip),
            new FakeVehicleRepository(CreateVehicle(trip.VehicleId, trip.OrganizationId, 45)),
            new FakeSeatRepository([]),
            new FakeReservationRepository([]),
            new FakeCurrentUserService(Guid.NewGuid()));

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(new GetTripSeatMapQuery { TripId = trip.Id }, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(TripStatus.Programado)]
    [InlineData(TripStatus.Publicado)]
    public async Task Handle_TripInOwnOrganization_ReturnsSeatMapRegardlessOfPublishStatus(TripStatus status)
    {
        // Unlike the public query, no Trip.Status == Publicado gate: the Planificador
        // legitimately wants to see this before/after publishing too.
        var organizationId = Guid.NewGuid();
        var trip = CreateTrip(organizationId, status);
        var vehicle = CreateVehicle(trip.VehicleId, organizationId, capacity: 4);
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

        var handler = new GetTripSeatMapQueryHandler(
            new FakeTripRepository(trip),
            new FakeVehicleRepository(vehicle),
            new FakeSeatRepository(seats),
            new FakeReservationRepository([reservation]),
            new FakeCurrentUserService(organizationId));

        var result = await handler.Handle(
            new GetTripSeatMapQuery { TripId = trip.Id }, TestContext.Current.CancellationToken);

        Assert.Equal(trip.Id, result.TripId);
        Assert.Equal(4, result.Capacity);
        Assert.Equal(trip.Price, result.Price);
        Assert.Equal([2], result.OccupiedSeatNumbers);
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var handler = new GetTripSeatMapQueryHandler(
            new FakeTripRepository(null),
            new FakeVehicleRepository(null),
            new FakeSeatRepository([]),
            new FakeReservationRepository([]),
            new FakeCurrentUserService(null));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new GetTripSeatMapQuery { TripId = Guid.NewGuid() }, TestContext.Current.CancellationToken));
    }
}
