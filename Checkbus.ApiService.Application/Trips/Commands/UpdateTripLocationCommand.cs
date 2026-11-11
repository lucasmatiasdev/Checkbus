using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    // DEMO-ONLY (odd/tasks/seguimiento-tiempo-real-demo.md) — one position ping from the
    // driver's own device while live tracking is on for this trip.
    public class UpdateTripLocationCommand : IRequest
    {
        public required Guid TripId { get; set; }
        public required double Latitude { get; set; }
        public required double Longitude { get; set; }
    }
}
