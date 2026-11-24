using Checkbus.ApiService.Domain.Entities.Trips;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface ITripRepository
    {

        Task AddAsync(Trip trip, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken);
        Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

        Task UpdateAsync(Trip trip, CancellationToken cancellationToken);
        Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken);

        Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(
            Guid vehicleId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken);
        Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(
            Guid driverId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken);

        Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken);
        Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken);

        Task UpdateStopDepartureTimesAsync(
            IReadOnlyDictionary<Guid, DateTime> departureTimesByStopId, CancellationToken cancellationToken);

        Task<bool> SlugExistsAsync(Guid organizationId, string slug, CancellationToken cancellationToken);

        Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken);

        Task<IReadOnlyList<Trip>> GetPublicUpcomingByOrganizationIdAsync(
            Guid organizationId,
            DateTime? dateFrom,
            DateTime? dateTo,
            string? eventName,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<Trip>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken)
            => throw new NotSupportedException("GetByEventIdAsync is not implemented by this ITripRepository.");
    }
}
