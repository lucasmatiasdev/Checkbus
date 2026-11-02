using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    // DEMO-ONLY. Deliberately NEVER throws for "trip doesn't exist" or "not tracking right now" —
    // both collapse into the exact same { LiveTrackingEnabled: false, ... } shape, so an anonymous
    // poller can never distinguish "wrong id" from "right id but nothing to show yet", same
    // posture as GetPublicTripDetailQueryHandler's shared TripNotFoundException, just expressed as
    // a 200 instead of a 404 because this is polled continuously and a 404 storm would be noisy.
    public class GetTripLocationQueryHandler : IRequestHandler<GetTripLocationQuery, TripLocationDto>
    {
        private static readonly TripLocationDto NotTracking = new() { LiveTrackingEnabled = false };

        private readonly ITripRepository _tripRepository;

        public GetTripLocationQueryHandler(ITripRepository tripRepository)
        {
            _tripRepository = tripRepository;
        }

        public async Task<TripLocationDto> Handle(GetTripLocationQuery request, CancellationToken cancellationToken)
        {
            var trip = await _tripRepository.GetByIdAsync(request.TripId, cancellationToken);
            if (trip is null || trip.Status != TripStatus.EnProceso || !trip.LiveTrackingEnabled)
            {
                return NotTracking;
            }

            return new TripLocationDto
            {
                LiveTrackingEnabled = true,
                Latitude = trip.CurrentLatitude,
                Longitude = trip.CurrentLongitude,
                UpdatedAt = trip.LocationUpdatedAt
            };
        }
    }
}
