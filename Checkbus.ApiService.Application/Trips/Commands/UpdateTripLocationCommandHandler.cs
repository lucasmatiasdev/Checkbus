using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Queries;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    public class UpdateTripLocationCommandHandler : IRequestHandler<UpdateTripLocationCommand>
    {
        private readonly ITripRepository _tripRepository;
        private readonly ICurrentUserService _currentUser;

        public UpdateTripLocationCommandHandler(ITripRepository tripRepository, ICurrentUserService currentUser)
        {
            _tripRepository = tripRepository;
            _currentUser = currentUser;
        }

        public async Task Handle(UpdateTripLocationCommand request, CancellationToken cancellationToken)
        {
            var trip = await GetTripBoardingListQueryHandler.AuthorizeDriverOwnsTripAsync(
                request.TripId, _tripRepository, _currentUser, cancellationToken);

            if (trip.Status != TripStatus.EnProceso)
            {
                throw new TripNotEligibleException(
                    "No se puede enviar la ubicación: el viaje no está en curso.");
            }

            if (!trip.LiveTrackingEnabled)
            {
                throw new TripNotEligibleException(
                    "No se puede enviar la ubicación: el seguimiento en tiempo real no está activado.");
            }

            trip.CurrentLatitude = request.Latitude;
            trip.CurrentLongitude = request.Longitude;
            trip.LocationUpdatedAt = DateTime.UtcNow;
            await _tripRepository.UpdateAsync(trip, cancellationToken);
        }
    }
}
