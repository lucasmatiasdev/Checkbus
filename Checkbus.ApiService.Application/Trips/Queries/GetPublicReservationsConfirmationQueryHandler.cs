using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Passengers;
using Checkbus.ApiService.Domain.Entities.Seats;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    // Public, read-only confirmation page (odd/tasks/reservas-publicas.md R5). Same posture as
    // GetPublicTripSeatMapQueryHandler/CreatePublicReservationCommandHandler: NO
    // ICurrentUserService, no org scoping — there is no authenticated caller on this public
    // surface. Unlike those two, there is also no Trip-level gate here at all:
    // Reservation.Id is already unguessable (128-bit), and it alone IS the access control for
    // this page — anyone holding one of these ids (from R4's create response) can view that
    // reservation's own confirmation, regardless of the parent Trip's current status. A
    // Reservation id that doesn't resolve is simply omitted, not an error (a stranger probing
    // random GUIDs just gets fewer rows back, never a 404/500 that would hint "that one is
    // real but something else is wrong"). The caller (PublicReservationsController) is the one
    // that turns an all-empty result into a 404, when NONE of the submitted ids resolved.
    public class GetPublicReservationsConfirmationQueryHandler
        : IRequestHandler<GetPublicReservationsConfirmationQuery, PublicReservationsConfirmationDto>
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly ISeatRepository _seatRepository;
        private readonly IPassengerRepository _passengerRepository;
        private readonly ITripRepository _tripRepository;
        private readonly IEventRepository _eventRepository;
        private readonly IUserRepository _userRepository;
        private readonly IVehicleRepository _vehicleRepository;

        public GetPublicReservationsConfirmationQueryHandler(
            IReservationRepository reservationRepository,
            ISeatRepository seatRepository,
            IPassengerRepository passengerRepository,
            ITripRepository tripRepository,
            IEventRepository eventRepository,
            IUserRepository userRepository,
            IVehicleRepository vehicleRepository)
        {
            _reservationRepository = reservationRepository;
            _seatRepository = seatRepository;
            _passengerRepository = passengerRepository;
            _tripRepository = tripRepository;
            _eventRepository = eventRepository;
            _userRepository = userRepository;
            _vehicleRepository = vehicleRepository;
        }

        public async Task<PublicReservationsConfirmationDto> Handle(
            GetPublicReservationsConfirmationQuery request, CancellationToken cancellationToken)
        {
            var confirmations = new List<ReservationConfirmationDto>();

            // Small, per-request caches: a batch of ids typically belongs to the same Trip
            // (the family-of-3 case), so this avoids re-fetching the same Trip/Event/Seats for
            // every entry without introducing a new bulk-by-ids repository method.
            var tripsById = new Dictionary<Guid, Trip?>();
            var eventNamesByEventId = new Dictionary<Guid, string?>();
            var seatsByVehicleId = new Dictionary<Guid, IReadOnlyList<Seat>>();
            var driversById = new Dictionary<Guid, User?>();
            var vehiclesById = new Dictionary<Guid, Vehicle?>();
            var routesByTripId = new Dictionary<Guid, Route?>();

            foreach (var reservationId in request.ReservationIds)
            {
                var reservation = await _reservationRepository.GetByIdAsync(reservationId, cancellationToken);
                if (reservation is null)
                {
                    // Garbage/unknown id — omitted, not an error. See class doc comment.
                    continue;
                }

                if (!tripsById.TryGetValue(reservation.TripId, out var trip))
                {
                    trip = await _tripRepository.GetByIdAsync(reservation.TripId, cancellationToken);
                    tripsById[reservation.TripId] = trip;
                }

                var passenger = await _passengerRepository.GetByIdAsync(reservation.PassengerId, cancellationToken);
                if (trip is null || passenger is null)
                {
                    // Should not happen given FK integrity, but a Reservation whose Trip or
                    // Passenger row is missing cannot be rendered meaningfully — omit it
                    // rather than crashing the whole confirmation page for the other ids in
                    // the batch.
                    continue;
                }

                if (!seatsByVehicleId.TryGetValue(trip.VehicleId, out var seats))
                {
                    seats = await _seatRepository.GetByVehicleIdAsync(trip.VehicleId, cancellationToken);
                    seatsByVehicleId[trip.VehicleId] = seats;
                }

                var seat = seats.FirstOrDefault(s => s.Id == reservation.SeatId);
                if (seat is null)
                {
                    continue;
                }

                if (!eventNamesByEventId.TryGetValue(trip.EventId, out var eventName))
                {
                    var @event = await _eventRepository.GetByIdAsync(trip.EventId, cancellationToken);
                    eventName = @event?.Name;
                    eventNamesByEventId[trip.EventId] = eventName;
                }

                if (!driversById.TryGetValue(trip.DriverId, out var driver))
                {
                    driver = await _userRepository.FindByIdAsync(trip.DriverId, cancellationToken);
                    driversById[trip.DriverId] = driver;
                }

                if (!vehiclesById.TryGetValue(trip.VehicleId, out var vehicle))
                {
                    vehicle = await _vehicleRepository.GetByIdAsync(trip.VehicleId, cancellationToken);
                    vehiclesById[trip.VehicleId] = vehicle;
                }

                if (!routesByTripId.TryGetValue(trip.Id, out var route))
                {
                    route = await _tripRepository.GetRouteByTripIdAsync(trip.Id, cancellationToken);
                    routesByTripId[trip.Id] = route;
                }

                confirmations.Add(new ReservationConfirmationDto
                {
                    ReservationId = reservation.Id,
                    SeatNumber = seat.Number,
                    PassengerName = passenger.Name,
                    PassengerSurname = passenger.Surname,
                    PassengerDocumentType = passenger.DocumentType,
                    PassengerDocumentNumber = passenger.DocumentNumber,
                    // Deliberately NOT passenger.Email / passenger.Phone — see
                    // PublicReservationsConfirmationDto's doc comment.
                    Price = reservation.Price,
                    Status = reservation.Status.ToString(),
                    EventName = eventName,
                    DepartureDate = trip.DepartureDate,
                    TripStatus = MapTripStatus(trip.Status),
                    DriverName = driver?.Name,
                    DriverSurname = driver?.Surname,
                    VehiclePatent = vehicle?.Patent,
                    EstimatedDuration = route?.EstimatedDuration,
                    DistanceKm = route?.DistanceKm,
                    ActualArrivalDate = trip.ActualArrivalDate
                });
            }

            return new PublicReservationsConfirmationDto { Reservations = confirmations };
        }

        // CU-11 replacement: a passenger-facing label, not the raw enum name — Programado and
        // Publicado both read as "Pendiente" from a passenger's point of view (the trip hasn't
        // departed yet either way).
        private static string MapTripStatus(TripStatus status) => status switch
        {
            TripStatus.Programado or TripStatus.Publicado => "Pendiente",
            TripStatus.EnProceso => "En curso",
            TripStatus.Completado => "Finalizado",
            TripStatus.Cancelado => "Cancelado",
            _ => status.ToString()
        };
    }
}
