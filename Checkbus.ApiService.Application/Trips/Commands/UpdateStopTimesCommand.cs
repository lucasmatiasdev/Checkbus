using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{

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
