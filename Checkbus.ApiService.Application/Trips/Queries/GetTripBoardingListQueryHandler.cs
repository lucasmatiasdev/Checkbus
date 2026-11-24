using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    public class GetTripBoardingListQueryHandler : IRequestHandler<GetTripBoardingListQuery, TripBoardingListDto>
    {
        private readonly ITripRepository _tripRepository;
        private readonly IBoardingRecordRepository _boardingRecordRepository;
        private readonly IReservationRepository _reservationRepository;
        private readonly ISeatRepository _seatRepository;
        private readonly IPassengerRepository _passengerRepository;
        private readonly ICurrentUserService _currentUser;

        public GetTripBoardingListQueryHandler(
            ITripRepository tripRepository,
            IBoardingRecordRepository boardingRecordRepository,
            IReservationRepository reservationRepository,
            ISeatRepository seatRepository,
            IPassengerRepository passengerRepository,
            ICurrentUserService currentUser)
        {
            _tripRepository = tripRepository;
            _boardingRecordRepository = boardingRecordRepository;
            _reservationRepository = reservationRepository;
            _seatRepository = seatRepository;
            _passengerRepository = passengerRepository;
            _currentUser = currentUser;
        }

        public async Task<TripBoardingListDto> Handle(GetTripBoardingListQuery request, CancellationToken cancellationToken)
        {
            var trip = await AuthorizeDriverOwnsTripAsync(request.TripId, _tripRepository, _currentUser, cancellationToken);

            await _boardingRecordRepository.EnsureForTripAsync(trip.Id, cancellationToken);

            var reservations = await _reservationRepository.GetByTripIdAsync(trip.Id, cancellationToken);
            if (reservations.Count == 0)
            {
                return new TripBoardingListDto { Passengers = [] };
            }

            var boardingRecords = await _boardingRecordRepository.GetByTripIdAsync(trip.Id, cancellationToken);
            var boardingRecordsByReservationId = boardingRecords.ToDictionary(b => b.ReservationId);

            var seats = await _seatRepository.GetByVehicleIdAsync(trip.VehicleId, cancellationToken);
            var seatsById = seats.ToDictionary(s => s.Id);

            var passengers = new List<BoardingPassengerDto>(reservations.Count);
            foreach (var reservation in reservations)
            {
                var passenger = await _passengerRepository.GetByIdAsync(reservation.PassengerId, cancellationToken);
                if (passenger is null || !seatsById.TryGetValue(reservation.SeatId, out var seat))
                {

                    continue;
                }

                boardingRecordsByReservationId.TryGetValue(reservation.Id, out var boardingRecord);

                passengers.Add(new BoardingPassengerDto
                {
                    ReservationId = reservation.Id,
                    SeatNumber = seat.Number,
                    PassengerName = passenger.Name,
                    PassengerSurname = passenger.Surname,
                    PassengerDocumentType = passenger.DocumentType,
                    PassengerDocumentNumber = passenger.DocumentNumber,

                    Status = boardingRecord?.Status ?? Domain.Enums.BoardingStatus.Pendiente
                });
            }

            return new TripBoardingListDto { Passengers = passengers };
        }

        internal static async Task<Trip> AuthorizeDriverOwnsTripAsync(
            Guid tripId,
            ITripRepository tripRepository,
            ICurrentUserService currentUser,
            CancellationToken cancellationToken)
        {
            var organizationId = currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");
            var driverId = currentUser.UserId
                ?? throw new InvalidOperationException("Authenticated caller has no UserId claim.");

            var trip = await tripRepository.GetByIdAsync(tripId, cancellationToken);
            if (trip is null || trip.OrganizationId != organizationId || trip.DriverId != driverId)
            {
                throw new TripNotFoundException();
            }

            return trip;
        }
    }
}
