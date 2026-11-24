using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Commands;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{

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

                throw new TripNotFoundException();
            }

            var vehicle = await _vehicleRepository.GetByIdAsync(trip.VehicleId, cancellationToken);
            var capacity = vehicle?.Capacity ?? trip.Capacity;

            await _seatRepository.EnsureSeatsForVehicleAsync(trip.VehicleId, capacity, cancellationToken);

            await _reservationRepository.ReleaseExpiredHoldsAsync(
                trip.Id, CreatePublicReservationCommandHandler.HoldExpirationMinutes, cancellationToken);

            var seats = await _seatRepository.GetByVehicleIdAsync(trip.VehicleId, cancellationToken);
            var reservations = await _reservationRepository.GetByTripIdAsync(trip.Id, cancellationToken);

            var occupiedSeatIds = reservations.Select(r => r.SeatId).ToHashSet();
            var occupiedSeatNumbers = seats
                .Where(s => occupiedSeatIds.Contains(s.Id))
                .Select(s => s.Number)
                .OrderBy(number => number)
                .ToList();

            var seatMapSeats = seats
                .Select(s => new SeatMapSeatDto { SeatId = s.Id, Number = s.Number })
                .OrderBy(s => s.Number)
                .ToList();

            return new PublicTripSeatMapDto
            {
                TripId = trip.Id,
                Capacity = capacity,
                Price = trip.Price,
                OccupiedSeatNumbers = occupiedSeatNumbers,
                Seats = seatMapSeats
            };
        }
    }
}
