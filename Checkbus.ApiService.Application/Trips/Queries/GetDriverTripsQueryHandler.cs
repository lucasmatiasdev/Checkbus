using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    public class GetDriverTripsQueryHandler : IRequestHandler<GetDriverTripsQuery, IReadOnlyList<TripDto>>
    {
        private readonly ITripRepository _tripRepository;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEventRepository _eventRepository;
        private readonly ICurrentUserService _currentUser;

        public GetDriverTripsQueryHandler(
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

        public async Task<IReadOnlyList<TripDto>> Handle(GetDriverTripsQuery request, CancellationToken cancellationToken)
        {
            var organizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");
            var driverId = _currentUser.UserId
                ?? throw new InvalidOperationException("Authenticated caller has no UserId claim.");

            var trips = await _tripRepository.GetAllByOrganizationAsync(organizationId, cancellationToken);

            // Scoped exclusively to the caller's own DriverId. Publicado (upcoming, boardable)
            // and EnProceso (the chofer already started it, CU-08) both stay visible here — once
            // the chofer starts their own trip it must not vanish from "Mis Viajes" before they
            // can reach Finalizar viaje. Programado/Cancelado/Completado are excluded: nothing
            // left for the driver to do on this screen for those.
            var driverTrips = trips
                .Where(t => t.DriverId == driverId && t.Status is TripStatus.Publicado or TripStatus.EnProceso)
                .OrderBy(t => t.DepartureDate)
                .ToList();

            if (driverTrips.Count == 0)
            {
                return [];
            }

            var vehicles = await _vehicleRepository.GetAllByOrganizationAsync(organizationId, cancellationToken);
            var vehiclesById = vehicles.ToDictionary(v => v.Id);

            var driver = await _userRepository.FindByIdAsync(driverId, cancellationToken);

            var eventsById = new Dictionary<Guid, Event>();
            foreach (var eventId in driverTrips.Select(t => t.EventId).Distinct())
            {
                var @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
                if (@event is not null)
                {
                    eventsById[eventId] = @event;
                }
            }

            return driverTrips.Select(trip =>
            {
                vehiclesById.TryGetValue(trip.VehicleId, out var vehicle);
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
            }).ToList();
        }
    }
}
