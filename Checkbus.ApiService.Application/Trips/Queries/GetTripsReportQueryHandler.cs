using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    public class GetTripsReportQueryHandler : IRequestHandler<GetTripsReportQuery, TripsReportDto>
    {
        private readonly ITripRepository _tripRepository;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEventRepository _eventRepository;
        private readonly ICurrentUserService _currentUser;

        public GetTripsReportQueryHandler(
            ITripRepository tripRepository,
            IVehicleRepository vehicleRepository,
            IUserRepository userRepository,
            IEventRepository eventRepository,
            ICurrentUserService currentUser)
        {
            _tripRepository = tripRepository;
            _vehicleRepository = vehicleRepository;
            _userRepository = userRepository;
            _eventRepository = eventRepository;
            _currentUser = currentUser;
        }

        public async Task<TripsReportDto> Handle(GetTripsReportQuery request, CancellationToken cancellationToken)
        {
            // Tenant comes exclusively from the validated claim — mirrors GetTripsQueryHandler.
            var organizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var trips = await _tripRepository.GetAllByOrganizationAsync(organizationId, cancellationToken);

            // Same in-memory filtering shape as GetMaintenanceRecordsQueryHandler — each
            // filter is "absent or matches", chained over the org-scoped list.
            var filtered = trips
                .Where(t => request.DateFrom is null || t.DepartureDate.Date >= request.DateFrom.Value.ToDateTime(TimeOnly.MinValue))
                .Where(t => request.DateTo is null || t.DepartureDate.Date <= request.DateTo.Value.ToDateTime(TimeOnly.MinValue))
                .Where(t => request.Status is null || t.Status == request.Status)
                .Where(t => request.DriverId is null || t.DriverId == request.DriverId)
                .Where(t => request.VehicleId is null || t.VehicleId == request.VehicleId)
                .ToList();

            // Vehicles/drivers referenced by this org's trips are always themselves in
            // this org (enforced at creation time) — mirrors GetTripsQueryHandler.
            var vehicles = await _vehicleRepository.GetAllByOrganizationAsync(organizationId, cancellationToken);
            var vehiclesById = vehicles.ToDictionary(v => v.Id);

            var users = await _userRepository.GetAllByOrganizationAsync(organizationId, cancellationToken);
            var usersById = users.ToDictionary(u => u.Id);

            var eventsById = new Dictionary<Guid, Event>();
            foreach (var eventId in filtered.Select(t => t.EventId).Distinct())
            {
                var @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
                if (@event is not null)
                {
                    eventsById[eventId] = @event;
                }
            }

            return new TripsReportDto
            {
                Trips = filtered.Select(t => MapToDto(t, vehiclesById, usersById, eventsById)).ToList(),
                Statistics = BuildStatistics(filtered)
            };
        }

        private static TripsReportStatisticsDto BuildStatistics(IReadOnlyList<Trip> trips) => new()
        {
            TotalTrips = trips.Count,
            TripsByStatus = trips
                .GroupBy(t => t.Status)
                .ToDictionary(g => g.Key, g => g.Count()),
            TotalPassengers = trips.Sum(t => t.Capacity - t.AvailableSeats),
            EstimatedRevenue = trips.Sum(t => t.Price * (t.Capacity - t.AvailableSeats))
        };

        // Duplicated from GetTripsQueryHandler.MapToDto (private there, and that file is
        // out of scope for this change) rather than introducing a shared mapper for one
        // other caller.
        private static TripDto MapToDto(
            Trip trip,
            Dictionary<Guid, Vehicle> vehiclesById,
            Dictionary<Guid, User> usersById,
            Dictionary<Guid, Event> eventsById)
        {
            vehiclesById.TryGetValue(trip.VehicleId, out var vehicle);
            usersById.TryGetValue(trip.DriverId, out var driver);
            eventsById.TryGetValue(trip.EventId, out var @event);

            return new TripDto
            {
                Id = trip.Id,
                VehicleId = trip.VehicleId,
                DriverId = trip.DriverId,
                EventId = trip.EventId,
                DepartureDate = trip.DepartureDate,
                ArrivalDate = trip.ArrivalDate,
                Capacity = trip.Capacity,
                AvailableSeats = trip.AvailableSeats,
                Price = trip.Price,
                Status = trip.Status,
                VehiclePatent = vehicle?.Patent,
                VehicleBrand = vehicle?.Brand,
                VehicleModel = vehicle?.Model,
                DriverName = driver?.Name,
                DriverSurname = driver?.Surname,
                EventName = @event?.Name
            };
        }
    }
}
