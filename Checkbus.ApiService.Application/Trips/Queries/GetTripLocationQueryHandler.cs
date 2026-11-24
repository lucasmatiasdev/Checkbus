using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{

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
