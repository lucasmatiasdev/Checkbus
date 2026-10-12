using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Commands;
using Checkbus.ApiService.Domain.Entities.BoardingRecords;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Trips;

namespace Checkbus.Tests.Boarding;

/// <summary>
/// Proves <see cref="CloseBoardingCommandHandler"/> ("Cerrar embarque",
/// odd/tasks/reservas-publicas.md Fase 3 / A4): same IDOR-safe ownership check as
/// <see cref="GetTripBoardingListQueryHandlerTests"/>, delegates the bulk
/// Pendiente -> Ausente transition to <see cref="IBoardingRecordRepository.CloseBoardingAsync"/>.
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
    }

    private sealed class FakeBoardingRecordRepository : IBoardingRecordRepository
    {
        public Guid? ClosedTripId { get; private set; }

        public Task EnsureForTripAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CloseBoardingCommandHandler.");

        public Task<IReadOnlyList<BoardingRecord>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CloseBoardingCommandHandler.");

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
    public async Task Handle_HappyPath_DelegatesToCloseBoardingAsyncForThisTrip()
    {
        var organizationId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var trip = CreateTrip(organizationId, driverId);
        var boardingRecordRepository = new FakeBoardingRecordRepository();

        var handler = new CloseBoardingCommandHandler(
            new FakeTripRepository(trip),
            boardingRecordRepository,
            new FakeCurrentUserService(organizationId, driverId));

        await handler.Handle(new CloseBoardingCommand { TripId = trip.Id }, TestContext.Current.CancellationToken);

        Assert.Equal(trip.Id, boardingRecordRepository.ClosedTripId);
    }

    [Fact]
    public async Task Handle_TripNotFound_ThrowsTripNotFound()
    {
        var handler = new CloseBoardingCommandHandler(
            new FakeTripRepository(null),
            new FakeBoardingRecordRepository(),
            new FakeCurrentUserService(Guid.NewGuid(), Guid.NewGuid()));

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(new CloseBoardingCommand { TripId = Guid.NewGuid() }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_TripBelongsToDifferentDriver_ThrowsTripNotFound()
    {
        var organizationId = Guid.NewGuid();
        var trip = CreateTrip(organizationId, Guid.NewGuid());

        var handler = new CloseBoardingCommandHandler(
            new FakeTripRepository(trip),
            new FakeBoardingRecordRepository(),
            new FakeCurrentUserService(organizationId, Guid.NewGuid()));

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(new CloseBoardingCommand { TripId = trip.Id }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_TripBelongsToDifferentOrganization_ThrowsTripNotFound()
    {
        var driverId = Guid.NewGuid();
        var trip = CreateTrip(Guid.NewGuid(), driverId);

        var handler = new CloseBoardingCommandHandler(
            new FakeTripRepository(trip),
            new FakeBoardingRecordRepository(),
            new FakeCurrentUserService(Guid.NewGuid(), driverId));

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(new CloseBoardingCommand { TripId = trip.Id }, TestContext.Current.CancellationToken));
    }
}
