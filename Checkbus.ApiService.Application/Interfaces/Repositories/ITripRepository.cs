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

        // Status-transition updates (e.g. PublishTripCommandHandler) only ever touch the Trip
        // row itself, never Route/Stops, so this is a plain single-entity update unlike AddAsync.
        Task UpdateAsync(Trip trip, CancellationToken cancellationToken);
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

        // Added for UpdateStopTimesCommandHandler (odd/tasks/mejoras-detalle-viaje.md Fase D) —
        // the first standalone Stop write (every prior Stop write only ever happened as part of
        // AddAsync's Trip/Route/Stops aggregate save). Stops are owned by the Trip aggregate, so
        // this stays on ITripRepository rather than a new IStopRepository. One SaveChangesAsync
        // call for the whole batch, same atomicity convention as every other batch write in this
        // codebase (e.g. IReservationRepository.CreateBatchAsync).
        Task UpdateStopDepartureTimesAsync(
            IReadOnlyDictionary<Guid, DateTime> departureTimesByStopId, CancellationToken cancellationToken);

        // Scoped by organization, not global — same criterion as Patent/DocumentNumber.
        // Used by CreateTripCommandHandler to disambiguate a generated slug before persisting.
        Task<bool> SlugExistsAsync(Guid organizationId, string slug, CancellationToken cancellationToken);

        // Public, unauthenticated lookup for the slug->Guid resolution endpoint. Does not leak
        // organization scoping since the slug itself carries no sensitive info and the caller
        // has no organization context (anonymous visitor).
        Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken);
    }
}
