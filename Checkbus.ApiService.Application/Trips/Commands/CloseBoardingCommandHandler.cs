using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Queries;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    public class CloseBoardingCommandHandler : IRequestHandler<CloseBoardingCommand>
    {
        private readonly ITripRepository _tripRepository;
        private readonly IBoardingRecordRepository _boardingRecordRepository;
        private readonly ICurrentUserService _currentUser;

        public CloseBoardingCommandHandler(
            ITripRepository tripRepository,
            IBoardingRecordRepository boardingRecordRepository,
            ICurrentUserService currentUser)
        {
            _tripRepository = tripRepository;
            _boardingRecordRepository = boardingRecordRepository;
            _currentUser = currentUser;
        }

        public async Task Handle(CloseBoardingCommand request, CancellationToken cancellationToken)
        {
            var trip = await GetTripBoardingListQueryHandler.AuthorizeDriverOwnsTripAsync(
                request.TripId, _tripRepository, _currentUser, cancellationToken);

            await _boardingRecordRepository.CloseBoardingAsync(trip.Id, cancellationToken);
        }
    }
}
