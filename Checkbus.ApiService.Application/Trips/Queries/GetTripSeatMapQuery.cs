using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    // Authenticated, org-scoped counterpart to GetPublicTripSeatMapQuery
    // (odd/tasks/mejoras-detalle-viaje.md Fase E1) — lets the Planificador who owns the Trip see
    // seat occupancy without going through the public page. No Trip.Status == Publicado gate,
    // unlike the public query: the Planificador legitimately wants to see this before/after
    // publishing too.
    public class GetTripSeatMapQuery : IRequest<TripSeatMapDto>
    {
        public required Guid TripId { get; set; }
    }
}
