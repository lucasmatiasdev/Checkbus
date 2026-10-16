using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    // No meaningful response body — the caller only needs to know the start succeeded (or which
    // error prevented it), same shape as PublishTripCommand/CancelTripCommand.
    public class StartTripCommand : IRequest
    {
        public required Guid Id { get; set; }
    }
}
