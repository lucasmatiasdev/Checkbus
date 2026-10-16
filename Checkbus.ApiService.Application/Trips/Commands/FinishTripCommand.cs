using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    // No meaningful response body — the caller only needs to know the finish succeeded (or
    // which error prevented it), same shape as StartTripCommand/CancelTripCommand.
    public class FinishTripCommand : IRequest
    {
        public required Guid Id { get; set; }
        public required DateTime ActualArrivalDate { get; set; }
        public string? Observations { get; set; }
    }
}
