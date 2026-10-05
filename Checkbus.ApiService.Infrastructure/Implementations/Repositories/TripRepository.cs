using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Trips;
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

        public async Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            return await _context.Trips
                .Where(t => t.OrganizationId == organizationId)
                .ToListAsync(cancellationToken);
        }

        // Standard interval-overlap condition: existing.DepartureDate <= arrivalDate &&
        // existing.ArrivalDate >= departureDate. A trip ending exactly when another starts
        // (or vice versa) counts as overlapping (boundary is inclusive on both sides).
        public async Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(
            Guid vehicleId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
        {
            return await _context.Trips
                .Where(t => t.VehicleId == vehicleId
                    && t.DepartureDate <= arrivalDate
                    && t.ArrivalDate >= departureDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(
            Guid driverId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
        {
            return await _context.Trips
                .Where(t => t.DriverId == driverId
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
    }
}
