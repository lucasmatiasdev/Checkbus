using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{

    public class UpdateStopTimesCommandHandler : IRequestHandler<UpdateStopTimesCommand>
    {
        private readonly ITripRepository _tripRepository;
        private readonly ICurrentUserService _currentUser;

        public UpdateStopTimesCommandHandler(ITripRepository tripRepository, ICurrentUserService currentUser)
        {
            _tripRepository = tripRepository;
            _currentUser = currentUser;
        }

        public async Task Handle(UpdateStopTimesCommand request, CancellationToken cancellationToken)
        {
            var callerOrganizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var trip = await _tripRepository.GetByIdAsync(request.TripId, cancellationToken);
            if (trip is null || trip.OrganizationId != callerOrganizationId)
            {

                throw new TripNotFoundException();
            }

            var route = await _tripRepository.GetRouteByTripIdAsync(trip.Id, cancellationToken);
            if (route is null)
            {
                throw new TripNotFoundException();
            }

            var stops = await _tripRepository.GetStopsByRouteIdAsync(route.Id, cancellationToken);
            var validStopIds = stops.Select(s => s.Id).ToHashSet();

            if (request.Stops.Any(s => !validStopIds.Contains(s.StopId)))
            {

                throw new TripNotFoundException();
            }

            var departureTimesByStopId = request.Stops.ToDictionary(
                s => s.StopId,
                s => DateTime.SpecifyKind(s.DepartureTime, DateTimeKind.Utc));

            await _tripRepository.UpdateStopDepartureTimesAsync(departureTimesByStopId, cancellationToken);
        }
    }
}
