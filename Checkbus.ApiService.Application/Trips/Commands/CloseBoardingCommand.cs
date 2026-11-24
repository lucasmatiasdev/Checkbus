using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{

    public class CloseBoardingCommand : IRequest
    {
        public required Guid TripId { get; set; }
    }
}
