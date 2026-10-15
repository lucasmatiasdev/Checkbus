using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.ApiService.Infrastructure.Implementations.Repositories
{
    public class TripRepository : ITripRepository
    {
        private readonly CheckbusDbContext _context;

        public TripRepository(CheckbusDbContext context)
        {
            _context = context;
        }

        // Trip, Route and all Stops are added to the same ChangeTracker and saved in one
        // SaveChangesAsync call, so they are persisted atomically as a single aggregate.
        public async Task AddAsync(Trip trip, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken)
        {
            _context.Trips.Add(trip);
            _context.Routes.Add(route);
            _context.Stops.AddRange(stops);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return _context.Trips.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        }

        public async Task UpdateAsync(Trip trip, CancellationToken cancellationToken)
        {
            _context.Trips.Update(trip);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            return await _context.Trips
                .Where(t => t.OrganizationId == organizationId)
                .ToListAsync(cancellationToken);
        }

        // Standard interval-overlap condition: existing.DepartureDate <= arrivalDate &&
        // existing.ArrivalDate >= departureDate. A trip ending exactly when another starts
        // (or vice versa) counts as overlapping (boundary is inclusive on both sides).
        // Cancelado is excluded: a cancelled trip must free its vehicle/driver for a new trip in
        // the same window (CU-05) — there is no separate "unassign" step since VehicleId/DriverId
        // are required, non-nullable FKs, so this exclusion IS the release mechanism.
        public async Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(
            Guid vehicleId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
        {
            return await _context.Trips
                .Where(t => t.VehicleId == vehicleId
                    && t.Status != TripStatus.Cancelado
                    && t.DepartureDate <= arrivalDate
                    && t.ArrivalDate >= departureDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(
            Guid driverId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
        {
            return await _context.Trips
                .Where(t => t.DriverId == driverId
                    && t.Status != TripStatus.Cancelado
                    && t.DepartureDate <= arrivalDate
                    && t.ArrivalDate >= departureDate)
                .ToListAsync(cancellationToken);
        }

        public Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
        {
            return _context.Routes.FirstOrDefaultAsync(r => r.TripId == tripId, cancellationToken);
        }

        public async Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken)
        {
            return await _context.Stops
                .Where(s => s.RouteId == routeId)
                .OrderBy(s => s.Order)
                .ToListAsync(cancellationToken);
        }

        // Loads exactly the given Stop rows and sets their DepartureTime, one SaveChangesAsync
        // for the whole batch — same atomicity convention as every other batch write in this
        // codebase.
        public async Task UpdateStopDepartureTimesAsync(
            IReadOnlyDictionary<Guid, DateTime> departureTimesByStopId, CancellationToken cancellationToken)
        {
            if (departureTimesByStopId.Count == 0)
            {
                return;
            }

            var stopIds = departureTimesByStopId.Keys.ToList();
            var stops = await _context.Stops
                .Where(s => stopIds.Contains(s.Id))
                .ToListAsync(cancellationToken);

            foreach (var stop in stops)
            {
                stop.DepartureTime = departureTimesByStopId[stop.Id];
                stop.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
