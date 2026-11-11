using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    // DEMO-ONLY (odd/tasks/seguimiento-tiempo-real-demo.md T2) — polled repeatedly by the public
    // passenger screen while a trip is in progress.
    public class GetTripLocationQuery : IRequest<TripLocationDto>
    {
        public required Guid TripId { get; set; }
    }
}
