using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Queries;
using Checkbus.ApiService.Domain.Entities.Trips;

namespace Checkbus.Tests.Trips;

public class ResolveTripSlugQueryHandlerTests
{
    private sealed class FakeTripRepository(Guid? resolvedId) : ITripRepository
    {
        public string? ReceivedSlug { get; private set; }

        public Task AddAsync(Trip trip, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ResolveTripSlugQueryHandler.");

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ResolveTripSlugQueryHandler.");

        public Task UpdateAsync(Trip trip, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ResolveTripSlugQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ResolveTripSlugQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(Guid vehicleId, DateTime fechaSalida, DateTime fechaLlegada, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ResolveTripSlugQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(Guid driverId, DateTime fechaSalida, DateTime fechaLlegada, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ResolveTripSlugQueryHandler.");

        public Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ResolveTripSlugQueryHandler.");

        public Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ResolveTripSlugQueryHandler.");

        public Task UpdateStopDepartureTimesAsync(
            IReadOnlyDictionary<Guid, DateTime> departureTimesByStopId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ResolveTripSlugQueryHandler.");

        public Task<bool> SlugExistsAsync(Guid organizationId, string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ResolveTripSlugQueryHandler.");

        public Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            ReceivedSlug = slug;
            return Task.FromResult(resolvedId);
        }
    }

    [Fact]
    public async Task Handle_ExistingSlug_ReturnsTripId()
    {
        var tripId = Guid.NewGuid();
        var repository = new FakeTripRepository(tripId);
        var handler = new ResolveTripSlugQueryHandler(repository);

        var result = await handler.Handle(new ResolveTripSlugQuery { Slug = "river-vs-boca-20261025" }, CancellationToken.None);

        Assert.Equal(tripId, result);
        Assert.Equal("river-vs-boca-20261025", repository.ReceivedSlug);
    }

    [Fact]
    public async Task Handle_UnknownSlug_ReturnsNull()
    {
        var repository = new FakeTripRepository(null);
        var handler = new ResolveTripSlugQueryHandler(repository);

        var result = await handler.Handle(new ResolveTripSlugQuery { Slug = "does-not-exist" }, CancellationToken.None);

        Assert.Null(result);
    }
}
