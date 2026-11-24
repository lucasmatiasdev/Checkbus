using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{

    public class ToggleTripLiveTrackingCommand : IRequest
    {
        public required Guid TripId { get; set; }
        public required bool Enabled { get; set; }
    }
}
