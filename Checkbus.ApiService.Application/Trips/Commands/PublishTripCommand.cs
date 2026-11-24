using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{

    public class PublishTripCommand : IRequest
    {
        public required Guid Id { get; set; }
    }
}
