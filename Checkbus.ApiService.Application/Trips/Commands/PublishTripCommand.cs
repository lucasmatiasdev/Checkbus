using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    // No meaningful response body — the caller only needs to know the publish succeeded (or
    // which error prevented it), same shape as ValidateVehicleDocumentCommand.
    public class PublishTripCommand : IRequest
    {
        public required Guid Id { get; set; }
    }
}
