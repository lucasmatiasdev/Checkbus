using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    // Same ISeatRepository/IReservationRepository occupancy computation as
    // GetPublicTripSeatMapQueryHandler, but org-scoped via ICurrentUserService instead of
    // gated by Trip.Status == Publicado — the Planificador legitimately wants to see this
    // before/after publishing too.
    public class GetTripSeatMapQueryHandler : IRequestHandler<GetTripSeatMapQuery, TripSeatMapDto>
    {
        private readonly ITripRepository _tripRepository;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly ISeatRepository _seatRepository;
        private readonly IReservationRepository _reservationRepository;
        private readonly ICurrentUserService _currentUser;

        public GetTripSeatMapQueryHandler(
            ITripRepository tripRepository,
            IVehicleRepository vehicleRepository,
            ISeatRepository seatRepository,
            IReservationRepository reservationRepository,
            ICurrentUserService currentUser)
        {
            _tripRepository = tripRepository;
            _vehicleRepository = vehicleRepository;
            _seatRepository = seatRepository;
            _reservationRepository = reservationRepository;
            _currentUser = currentUser;
        }

        public async Task<TripSeatMapDto> Handle(GetTripSeatMapQuery request, CancellationToken cancellationToken)
        {
            var organizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var trip = await _tripRepository.GetByIdAsync(request.TripId, cancellationToken);
            if (trip is null || trip.OrganizationId != organizationId)
            {
                // IDOR-safe: a trip belonging to another organization looks identical to a
                // missing one — cross-tenant existence is never leaked to the caller.
                throw new TripNotFoundException();
            }

            var vehicle = await _vehicleRepository.GetByIdAsync(trip.VehicleId, cancellationToken);
            var capacity = vehicle?.Capacity ?? trip.Capacity;

            // Lazily, idempotently backfills Seat rows, same as GetPublicTripSeatMapQueryHandler
            // — a no-op read when seats already exist, so it's safe here too.
            await _seatRepository.EnsureSeatsForVehicleAsync(trip.VehicleId, capacity, cancellationToken);

            var seats = await _seatRepository.GetByVehicleIdAsync(trip.VehicleId, cancellationToken);
            var reservations = await _reservationRepository.GetByTripIdAsync(trip.Id, cancellationToken);

            var occupiedSeatIds = reservations.Select(r => r.SeatId).ToHashSet();
            var occupiedSeatNumbers = seats
                .Where(s => occupiedSeatIds.Contains(s.Id))
                .Select(s => s.Number)
                .OrderBy(number => number)
                .ToList();

            return new TripSeatMapDto
            {
                TripId = trip.Id,
                Capacity = capacity,
                Price = trip.Price,
                OccupiedSeatNumbers = occupiedSeatNumbers
            };
        }
    }
}
