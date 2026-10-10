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

            // Lazily, idempotently backfill any BoardingRecord rows this Trip's Reservations
            // never got — safe to call on every boarding-screen open (see
            // IBoardingRecordRepository.EnsureForTripAsync's doc comment).
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
                    // Should not happen given FK integrity, but a Reservation whose
                    // Passenger/Seat row is missing cannot be rendered meaningfully — omit it
                    // rather than crashing the whole boarding list for the rest.
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
                    // Deliberately NOT passenger.Email / passenger.Phone — see
                    // TripBoardingListDto's doc comment.
                    Status = boardingRecord?.Status ?? Domain.Enums.BoardingStatus.Pendiente
                });
            }

            return new TripBoardingListDto { Passengers = passengers };
        }

        // Shared by ValidateBoardingCommandHandler and CloseBoardingCommandHandler — every
        // action on this boarding surface needs the exact same "the Trip exists, belongs to
        // the caller's organization, and its DriverId is the caller's own id" rule. IDOR-safe:
        // a non-existent Trip, a cross-tenant Trip, and someone else's Trip are all
        // indistinguishable to the caller (same TripNotFoundException CreatePublicReservationCommandHandler
        // throws for "exists but not published" — "exists but not mine" gets the same treatment).
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
