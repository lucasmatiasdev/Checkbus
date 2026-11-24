using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    public class GetTripsQueryHandler : IRequestHandler<GetTripsQuery, IReadOnlyList<TripDto>>
    {
        private readonly ITripRepository _tripRepository;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEventRepository _eventRepository;
        private readonly ICurrentUserService _currentUser;

        public GetTripsQueryHandler(
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

        public async Task<IReadOnlyList<TripDto>> Handle(GetTripsQuery request, CancellationToken cancellationToken)
        {

            var organizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var trips = await _tripRepository.GetAllByOrganizationAsync(organizationId, cancellationToken);

            var vehicles = await _vehicleRepository.GetAllByOrganizationAsync(organizationId, cancellationToken);
            var vehiclesById = vehicles.ToDictionary(v => v.Id);

            var users = await _userRepository.GetAllByOrganizationAsync(organizationId, cancellationToken);
            var usersById = users.ToDictionary(u => u.Id);

            var eventsById = new Dictionary<Guid, Event>();
            foreach (var eventId in trips.Select(t => t.EventId).Distinct())
            {
                var @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
                if (@event is not null)
                {
                    eventsById[eventId] = @event;
                }
            }

            return trips.Select(t => MapToDto(t, vehiclesById, usersById, eventsById)).ToList();
        }

        private static TripDto MapToDto(
            Domain.Entities.Trips.Trip trip,
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
