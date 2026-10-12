using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    // Deliberately takes NO ICurrentUserService — same public, unauthenticated posture as
    // GetPublicTripSeatMapQueryHandler (odd/tasks/reservas-publicas.md R3). The ONLY visibility
    // gate is Trip.Status == Publicado — a Trip that doesn't exist and a real Trip in any other
    // status both throw the same TripNotFoundException (404), so a stranger probing random GUIDs
    // can never distinguish "wrong id" from "exists but not published" (odd/tasks/
    // detalle-reserva-publica.md T2 reuses this exact rationale, not a new one).
    public class GetPublicTripDetailQueryHandler : IRequestHandler<GetPublicTripDetailQuery, PublicTripDetailDto>
    {
        private readonly ITripRepository _tripRepository;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IReservationRepository _reservationRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IEventRepository _eventRepository;
        private readonly ILocationRepository _locationRepository;

        public GetPublicTripDetailQueryHandler(
            ITripRepository tripRepository,
            IVehicleRepository vehicleRepository,
            IReservationRepository reservationRepository,
            IOrganizationRepository organizationRepository,
            IEventRepository eventRepository,
            ILocationRepository locationRepository)
        {
            _tripRepository = tripRepository;
            _vehicleRepository = vehicleRepository;
            _reservationRepository = reservationRepository;
            _organizationRepository = organizationRepository;
            _eventRepository = eventRepository;
            _locationRepository = locationRepository;
        }

        public async Task<PublicTripDetailDto> Handle(GetPublicTripDetailQuery request, CancellationToken cancellationToken)
        {
            var trip = await _tripRepository.GetByIdAsync(request.TripId, cancellationToken);
            if (trip is null || trip.Status != TripStatus.Publicado)
            {
                // Same exception, same message, for both "truly doesn't exist" and "exists but
                // not published" — see class doc comment above.
                throw new TripNotFoundException();
            }

            var vehicle = await _vehicleRepository.GetByIdAsync(trip.VehicleId, cancellationToken);
            var capacity = vehicle?.Capacity ?? trip.Capacity;

            // Only the COUNT of existing reservations is needed here — unlike
            // GetPublicTripSeatMapQueryHandler, this endpoint never resolves/seeds individual
            // Seat rows, it has no per-seat UI to drive.
            var reservations = await _reservationRepository.GetByTripIdAsync(trip.Id, cancellationToken);
            var availableSeats = capacity - reservations.Count;

            var organizationName = await _organizationRepository.GetNameByIdAsync(trip.OrganizationId, cancellationToken);
            var @event = await _eventRepository.GetByIdAsync(trip.EventId, cancellationToken);

            var route = await _tripRepository.GetRouteByTripIdAsync(trip.Id, cancellationToken);
            var stopDtos = new List<PublicTripDetailDto.StopDto>();

            if (route is not null)
            {
                var stops = await _tripRepository.GetStopsByRouteIdAsync(route.Id, cancellationToken);

                foreach (var stop in stops)
                {
                    var location = await _locationRepository.GetByIdAsync(stop.LocationId, cancellationToken);

                    stopDtos.Add(new PublicTripDetailDto.StopDto
                    {
                        Order = stop.Order,
                        Type = stop.Type,
                        Name = location?.Name ?? string.Empty,
                        Address = location?.Address ?? string.Empty,
                        Latitude = location?.Latitude ?? 0,
                        Longitude = location?.Longitude ?? 0,
                        DepartureTime = stop.DepartureTime
                    });
                }
            }

            return new PublicTripDetailDto
            {
                TripId = trip.Id,
                OrganizationName = organizationName ?? string.Empty,
                EventId = trip.EventId,
                EventName = @event?.Name ?? string.Empty,
                EventHasImage = @event?.ImageKey is not null,
                Price = trip.Price,
                AvailableSeats = availableSeats,
                DepartureDate = trip.DepartureDate,
                ArrivalDate = trip.ArrivalDate,
                Stops = stopDtos
            };
        }
    }
}
