using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    public class PublishTripCommandHandler : IRequestHandler<PublishTripCommand>
    {
        private readonly ITripRepository _tripRepository;
        private readonly ICurrentUserService _currentUser;

        public PublishTripCommandHandler(ITripRepository tripRepository, ICurrentUserService currentUser)
        {
            _tripRepository = tripRepository;
            _currentUser = currentUser;
        }

        public async Task Handle(PublishTripCommand request, CancellationToken cancellationToken)
        {

            var callerOrganizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var trip = await _tripRepository.GetByIdAsync(request.Id, cancellationToken);
            if (trip is null || trip.OrganizationId != callerOrganizationId)
            {

                throw new TripNotFoundException();
            }

            if (trip.Status != TripStatus.Programado)
            {
                throw new TripNotEligibleException(
                    "El viaje no puede publicarse: solo un viaje en estado Programado puede publicarse.");
            }

            trip.Status = TripStatus.Publicado;
            trip.UpdatedAt = DateTime.UtcNow;

            await _tripRepository.UpdateAsync(trip, cancellationToken);
        }
    }
}
