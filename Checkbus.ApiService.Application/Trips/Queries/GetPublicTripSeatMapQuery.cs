using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    // Carries only TripId — the Trip.Status == Publicado check inside the handler is the sole
    // visibility gate (no ICurrentUserService, no org scoping: there is no authenticated
    // caller on this public, unauthenticated surface — see
    // odd/tasks/reservas-publicas.md R3 and GetPublicTripSeatMapQueryHandler's doc comment).
    public class GetPublicTripSeatMapQuery : IRequest<PublicTripSeatMapDto>
    {
        public required Guid TripId { get; set; }
    }
}
