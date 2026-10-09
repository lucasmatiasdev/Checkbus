using MediatR;
using Checkbus.ApiService.Application.Events.Queries;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    // Added during T4 (odd/tasks/detalle-reserva-publica.md) — T1's GET api/Events/{id}/image is
    // Planificador/Administrador-only, so it cannot serve the Step 1 event image to an anonymous
    // passenger. This query resolves the image through the Trip instead, reusing the exact same
    // Trip.Status == Publicado visibility gate as every other action on this public surface,
    // rather than exposing a generic "any Event's image by id" anonymous endpoint.
    public class GetPublicTripEventImageQuery : IRequest<EventImageFileDto>
    {
        public required Guid TripId { get; set; }
    }
}
