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

        public Task<bool> SlugExistsAsync(Guid organizationId, string slug, CancellationToken cancellationToken)
        {
            return _context.Trips.AnyAsync(t => t.OrganizationId == organizationId && t.Slug == slug, cancellationToken);
        }

        public async Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            var trip = await _context.Trips
                .Where(t => t.Slug == slug)
                .Select(t => new { t.Id })
                .FirstOrDefaultAsync(cancellationToken);

            return trip?.Id;
        }

        public async Task<IReadOnlyList<Trip>> GetPublicUpcomingByOrganizationIdAsync(
            Guid organizationId,
            DateTime? dateFrom,
            DateTime? dateTo,
            string? eventName,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var query = _context.Trips
                .Where(t => t.OrganizationId == organizationId
                    && t.Status == TripStatus.Publicado
                    && t.DepartureDate >= now);

            if (dateFrom.HasValue)
            {
                query = query.Where(t => t.DepartureDate >= dateFrom.Value);
            }

            if (dateTo.HasValue)
            {
                query = query.Where(t => t.DepartureDate <= dateTo.Value);
            }

            if (!string.IsNullOrWhiteSpace(eventName))
            {

                var normalizedEventName = eventName.ToLower();
                var matchingEventIds = _context.Events
                    .Where(e => e.Name.ToLower().Contains(normalizedEventName))
                    .Select(e => e.Id);

                query = query.Where(t => matchingEventIds.Contains(t.EventId));
            }

            return await query
                .OrderBy(t => t.DepartureDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Trip>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken)
        {
            return await _context.Trips
                .Where(t => t.EventId == eventId)
                .ToListAsync(cancellationToken);
        }
    }
}
