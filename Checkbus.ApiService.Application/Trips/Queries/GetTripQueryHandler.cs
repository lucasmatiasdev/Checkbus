using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    public class GetTripQueryHandler : IRequestHandler<GetTripQuery, TripDetailDto>
    {
        private readonly ITripRepository _tripRepository;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEventRepository _eventRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly ICurrentUserService _currentUser;

        public GetTripQueryHandler(
            ITripRepository tripRepository,
            IVehicleRepository vehicleRepository,
            IUserRepository userRepository,
            IEventRepository eventRepository,
            ILocationRepository locationRepository,
            ICurrentUserService currentUser)
        {
            _tripRepository = tripRepository;
            _vehicleRepository = vehicleRepository;
            _userRepository = userRepository;
            _eventRepository = eventRepository;
            _locationRepository = locationRepository;
            _currentUser = currentUser;
        }

        public async Task<TripDetailDto> Handle(GetTripQuery request, CancellationToken cancellationToken)
        {
            var organizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var trip = await _tripRepository.GetByIdAsync(request.Id, cancellationToken);
            if (trip is null || trip.OrganizationId != organizationId)
            {
                // IDOR-safe: a trip belonging to another organization looks identical to a
                // missing one — cross-tenant existence is never leaked to the caller.
                throw new TripNotFoundException();
            }

            var vehicle = await _vehicleRepository.GetByIdAsync(trip.VehicleId, cancellationToken);
            var driver = await _userRepository.FindByIdAsync(trip.DriverId, cancellationToken);
            var @event = await _eventRepository.GetByIdAsync(trip.EventId, cancellationToken);

            var route = await _tripRepository.GetRouteByTripIdAsync(trip.Id, cancellationToken);
            var stopDtos = new List<TripDetailDto.StopDto>();

            if (route is not null)
            {
                var stops = await _tripRepository.GetStopsByRouteIdAsync(route.Id, cancellationToken);

                foreach (var stop in stops)
                {
                    var location = await _locationRepository.GetByIdAsync(stop.LocationId, cancellationToken);

                    stopDtos.Add(new TripDetailDto.StopDto
                    {
                        Order = stop.Order,
                        Type = stop.Type,
                        Name = location?.Name ?? string.Empty,
                        Address = location?.Address ?? string.Empty,
                        Latitude = location?.Latitude ?? 0,
                        Longitude = location?.Longitude ?? 0
                    });
                }
            }

            return new TripDetailDto
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
                EventName = @event?.Name,
                Route = new TripDetailDto.RouteDto
                {
                    EstimatedDuration = route?.EstimatedDuration ?? TimeSpan.Zero,
                    DistanceKm = route?.DistanceKm ?? 0,
                    Stops = stopDtos
                }
            };
        }
    }
}
