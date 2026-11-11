using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    // DEMO-ONLY (odd/tasks/seguimiento-tiempo-real-demo.md) — the driver turns their own
    // device's live position sharing on/off for a trip they are currently driving.
    public class ToggleTripLiveTrackingCommand : IRequest
    {
        public required Guid TripId { get; set; }
        public required bool Enabled { get; set; }
    }
}
