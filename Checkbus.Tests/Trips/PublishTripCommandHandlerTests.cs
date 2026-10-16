using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Commands;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Trips;

namespace Checkbus.Tests.Trips;

public class PublishTripCommandHandlerTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();

    private sealed class FakeCurrentUserService(Guid? organizationId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.NewGuid();
        public Guid? OrganizationId => organizationId;
        public string? Role => "Planificador";
        public string? Email => "planificador@checkbus-demo.com";
    }

    private sealed class FakeTripRepository(Trip? trip) : ITripRepository
    {
        public Trip? UpdatedTrip { get; private set; }

        public Task AddAsync(Trip trip, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by PublishTripCommandHandler.");

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(trip);

        public Task UpdateAsync(Trip trip, CancellationToken cancellationToken)
        {
            UpdatedTrip = trip;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by PublishTripCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(
            Guid vehicleId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by PublishTripCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(
            Guid driverId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by PublishTripCommandHandler.");

        public Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by PublishTripCommandHandler.");

        public Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by PublishTripCommandHandler.");

        public Task UpdateStopDepartureTimesAsync(
            IReadOnlyDictionary<Guid, DateTime> departureTimesByStopId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by PublishTripCommandHandler.");

        public Task<bool> SlugExistsAsync(Guid organizationId, string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by PublishTripCommandHandler.");

        public Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by PublishTripCommandHandler.");
    }

    private static Trip CreateTrip(TripStatus status = TripStatus.Programado, Guid? organizationId = null) => new()
    {
        Id = Guid.NewGuid(),
        Slug = Guid.NewGuid().ToString(),
        OrganizationId = organizationId ?? OrganizationId,
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

    private static PublishTripCommandHandler BuildHandler(Trip? trip, Guid? callerOrganizationId = null) =>
        new(new FakeTripRepository(trip), new FakeCurrentUserService(callerOrganizationId ?? OrganizationId));

    [Fact]
    public async Task Handle_ProgramadoTrip_MovesToPublicadoAndPersists()
    {
        var trip = CreateTrip();
        var repository = new FakeTripRepository(trip);
        var handler = new PublishTripCommandHandler(repository, new FakeCurrentUserService(OrganizationId));

        await handler.Handle(new PublishTripCommand { Id = trip.Id }, TestContext.Current.CancellationToken);

        Assert.Equal(TripStatus.Publicado, trip.Status);
        Assert.NotNull(repository.UpdatedTrip);
        Assert.Equal(TripStatus.Publicado, repository.UpdatedTrip!.Status);
    }

    [Theory]
    [InlineData(TripStatus.EnProceso)]
    [InlineData(TripStatus.Completado)]
    [InlineData(TripStatus.Cancelado)]
    [InlineData(TripStatus.Publicado)]
    public async Task Handle_NonProgramadoTrip_ThrowsTripNotEligible(TripStatus status)
    {
        var trip = CreateTrip(status);
        var handler = BuildHandler(trip);

        await Assert.ThrowsAsync<TripNotEligibleException>(() =>
            handler.Handle(new PublishTripCommand { Id = trip.Id }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_TripNotFound_ThrowsTripNotFound()
    {
        var handler = BuildHandler(null);

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(new PublishTripCommand { Id = Guid.NewGuid() }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_TripBelongsToDifferentOrganization_ThrowsTripNotFound()
    {
        var otherOrgTrip = CreateTrip(organizationId: Guid.NewGuid());
        var handler = BuildHandler(otherOrgTrip);

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(new PublishTripCommand { Id = otherOrgTrip.Id }, TestContext.Current.CancellationToken));
    }
}
