using Checkbus.ApiService.Domain.Entities.Trips;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface ITripRepository
    {
        // Trip, its Route and all Stops are added to the same ChangeTracker and saved in
        // one SaveChangesAsync call, so they are persisted atomically as a single
        // aggregate — same shape as IVehicleDiagnosticRepository.AddAsync(diagnostic, components).
        Task AddAsync(Trip trip, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken);
        Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken);

        // Both overlap queries exist for R5's eligibility checks: an existing Trip
        // overlaps the given range when existing.DepartureDate <= arrivalDate &&
        // existing.ArrivalDate >= departureDate (standard interval-overlap condition).
        Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(
            Guid vehicleId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken);
        Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(
            Guid driverId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken);

        // Added in R5 so GetTripQueryHandler can render a Trip's route/stops detail — R1 only
        // needed to persist the aggregate, never to read Route/Stops back.
        Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken);
        Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken);
    }
}
