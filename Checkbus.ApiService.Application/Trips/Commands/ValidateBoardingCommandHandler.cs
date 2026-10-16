using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Queries;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Boarding;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    public class ValidateBoardingCommandHandler : IRequestHandler<ValidateBoardingCommand>
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly ITripRepository _tripRepository;
        private readonly IBoardingRecordRepository _boardingRecordRepository;
        private readonly ICurrentUserService _currentUser;

        public ValidateBoardingCommandHandler(
            IReservationRepository reservationRepository,
            ITripRepository tripRepository,
            IBoardingRecordRepository boardingRecordRepository,
            ICurrentUserService currentUser)
        {
            _reservationRepository = reservationRepository;
            _tripRepository = tripRepository;
            _boardingRecordRepository = boardingRecordRepository;
            _currentUser = currentUser;
        }

        public async Task Handle(ValidateBoardingCommand request, CancellationToken cancellationToken)
        {
            var reservation = await _reservationRepository.GetByIdAsync(request.ReservationId, cancellationToken)
                ?? throw new BoardingRecordNotFoundException(
                    "The submitted code does not match any reservation for a trip you drive.");

            // IDOR-safe ownership check, same rule GetTripBoardingListQueryHandler uses — a
            // Reservation belonging to a Trip the caller doesn't drive must never be
            // distinguishable from one that genuinely doesn't exist.
            await GetTripBoardingListQueryHandler.AuthorizeDriverOwnsTripAsync(
                reservation.TripId, _tripRepository, _currentUser, cancellationToken);

            // CU-12: the BoardingRecord alone doesn't know about cancellation — a Cancelada
            // reservation (or one still PendientePago) must never be markable Abordado.
            if (reservation.Status != ReservationStatus.Confirmada)
            {
                throw new ReservationNotEligibleForBoardingException();
            }

            await _boardingRecordRepository.ValidateAsync(request.ReservationId, request.Method, cancellationToken);
        }
    }
}
