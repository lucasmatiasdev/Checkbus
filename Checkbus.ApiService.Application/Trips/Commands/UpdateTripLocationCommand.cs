using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{

    public class UpdateTripLocationCommand : IRequest
    {
        public required Guid TripId { get; set; }
        public required double Latitude { get; set; }
        public required double Longitude { get; set; }
    }
}
