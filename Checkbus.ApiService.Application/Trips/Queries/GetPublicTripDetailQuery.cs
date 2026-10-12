using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    // Carries only TripId — same public, unauthenticated posture as GetPublicTripSeatMapQuery
    // (odd/tasks/detalle-reserva-publica.md T2, reusing the exact gate reservas-publicas R3
    // established): no ICurrentUserService, no org scoping, Trip.Status == Publicado inside the
    // handler is the sole visibility gate.
    public class GetPublicTripDetailQuery : IRequest<PublicTripDetailDto>
    {
        public required Guid TripId { get; set; }
    }
}
