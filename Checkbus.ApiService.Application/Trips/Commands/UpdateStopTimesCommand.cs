using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    // Edits the placeholder DepartureTime computed at Trip creation
    // (odd/tasks/mejoras-detalle-viaje.md Fase D3). TripId is deliberately the only identifier
    // trusted from the route; every submitted StopId is re-validated against that Trip's own
    // Route server-side (never trusted blindly from the client) — see
    // UpdateStopTimesCommandHandler.
    public class UpdateStopTimesCommand : IRequest
    {
        public required Guid TripId { get; set; }
        public required IReadOnlyList<StopDepartureTimeUpdate> Stops { get; set; }
    }

    public class StopDepartureTimeUpdate
    {
        public required Guid StopId { get; set; }
        public required DateTime DepartureTime { get; set; }
    }
}
