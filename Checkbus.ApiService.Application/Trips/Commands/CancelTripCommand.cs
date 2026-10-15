using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    // No meaningful response body — the caller only needs to know the cancellation succeeded
    // (or which error prevented it), same shape as PublishTripCommand.
    public class CancelTripCommand : IRequest
    {
        public required Guid Id { get; set; }
        public required TripCancellationReason Reason { get; set; }

        // Only required when Reason == Otro — see CancelTripCommandValidator.
        public string? Description { get; set; }
    }
}
