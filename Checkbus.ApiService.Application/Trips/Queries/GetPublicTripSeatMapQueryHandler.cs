using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    // Deliberately takes NO ICurrentUserService: this is the project's first business-domain
    // unauthenticated surface (odd/tasks/reservas-publicas.md R3), so there is no
    // authenticated caller and no OrganizationId to scope by. The ONLY visibility gate is
    // Trip.Status == Publicado — a Trip that doesn't exist and a real Trip in any other
    // status both throw the same TripNotFoundException (404), so a stranger probing random
    // GUIDs can never distinguish "wrong id" from "exists but not published" (same IDOR-safe
    // rationale as every other *NotFoundException in this codebase, just without the
    // cross-tenant angle since there is no tenant here).
    public class GetPublicTripSeatMapQueryHandler : IRequestHandler<GetPublicTripSeatMapQuery, PublicTripSeatMapDto>
    {
        private readonly ITripRepository _tripRepository;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly ISeatRepository _seatRepository;
        private readonly IReservationRepository _reservationRepository;

        public GetPublicTripSeatMapQueryHandler(
            ITripRepository tripRepository,
            IVehicleRepository vehicleRepository,
            ISeatRepository seatRepository,
            IReservationRepository reservationRepository)
        {
            _tripRepository = tripRepository;
            _vehicleRepository = vehicleRepository;
            _seatRepository = seatRepository;
            _reservationRepository = reservationRepository;
        }

        public async Task<PublicTripSeatMapDto> Handle(GetPublicTripSeatMapQuery request, CancellationToken cancellationToken)
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

            // Lazily, idempotently backfills Seat rows the first time this published Trip's
            // seat map is requested — nothing in this codebase creates them yet (not Vehicle
            // registration, not PublishTripCommandHandler). See ISeatRepository's doc comment.
            await _seatRepository.EnsureSeatsForVehicleAsync(trip.VehicleId, capacity, cancellationToken);

            var seats = await _seatRepository.GetByVehicleIdAsync(trip.VehicleId, cancellationToken);
            var reservations = await _reservationRepository.GetByTripIdAsync(trip.Id, cancellationToken);

            var occupiedSeatIds = reservations.Select(r => r.SeatId).ToHashSet();
            var occupiedSeatNumbers = seats
                .Where(s => occupiedSeatIds.Contains(s.Id))
                .Select(s => s.Number)
                .OrderBy(number => number)
                .ToList();

            return new PublicTripSeatMapDto
            {
                TripId = trip.Id,
                Capacity = capacity,
                Price = trip.Price,
                OccupiedSeatNumbers = occupiedSeatNumbers
            };
        }
    }
}
