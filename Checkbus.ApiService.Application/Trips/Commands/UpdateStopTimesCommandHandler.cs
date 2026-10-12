using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    // Planificador/Administrador-only is enforced at the controller; this handler verifies
    // tenant ownership of the target Trip (same org-ownership check GetTripQueryHandler already
    // does) and that every submitted StopId truly belongs to that Trip's own Route — the
    // client's StopId list is never trusted blindly. Both "Trip doesn't exist"/"wrong org" and
    // "StopId doesn't belong to this Trip's Route" throw the same TripNotFoundException,
    // IDOR-safe like every other handler in this module.
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
                // IDOR-safe: cross-tenant existence is never leaked to the caller.
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
                // Not trusting the client's StopId list blindly — a StopId that doesn't belong
                // to this Trip's Route is treated the same as a not-found Trip.
                throw new TripNotFoundException();
            }

            // Checkbus.Web's pickers produce Kind=Unspecified DateTimes; Npgsql's "timestamp
            // with time zone" columns only accept Utc, same normalization CreateTripCommandHandler
            // already does for DepartureDate/ArrivalDate.
            var departureTimesByStopId = request.Stops.ToDictionary(
                s => s.StopId,
                s => DateTime.SpecifyKind(s.DepartureTime, DateTimeKind.Utc));

            await _tripRepository.UpdateStopDepartureTimesAsync(departureTimesByStopId, cancellationToken);
        }
    }
}
