using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Queries;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    public class ToggleTripLiveTrackingCommandHandler : IRequestHandler<ToggleTripLiveTrackingCommand>
    {
        private readonly ITripRepository _tripRepository;
        private readonly ICurrentUserService _currentUser;

        public ToggleTripLiveTrackingCommandHandler(ITripRepository tripRepository, ICurrentUserService currentUser)
        {
            _tripRepository = tripRepository;
            _currentUser = currentUser;
        }

        public async Task Handle(ToggleTripLiveTrackingCommand request, CancellationToken cancellationToken)
        {
            // Same IDOR-safe ownership rule as the rest of the boarding surface.
            var trip = await GetTripBoardingListQueryHandler.AuthorizeDriverOwnsTripAsync(
                request.TripId, _tripRepository, _currentUser, cancellationToken);

            if (trip.Status != TripStatus.EnProceso)
            {
                throw new TripNotEligibleException(
                    "El seguimiento en tiempo real solo puede activarse mientras el viaje está en curso.");
            }

            trip.LiveTrackingEnabled = request.Enabled;
            if (!request.Enabled)
            {
                // Clear the stale point rather than leaving a frozen pin on the map once
                // sharing stops — the viewer's screen hides the map when this is false anyway,
                // but this keeps the data itself from implying a position that may be minutes old.
                trip.CurrentLatitude = null;
                trip.CurrentLongitude = null;
                trip.LocationUpdatedAt = null;
            }

            trip.UpdatedAt = DateTime.UtcNow;
            await _tripRepository.UpdateAsync(trip, cancellationToken);
        }
    }
}
