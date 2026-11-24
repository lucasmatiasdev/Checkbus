using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{

    public class StartTripCommand : IRequest
    {
        public required Guid Id { get; set; }
    }
}
