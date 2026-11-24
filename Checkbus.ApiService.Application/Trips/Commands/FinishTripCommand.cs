using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{

    public class FinishTripCommand : IRequest
    {
        public required Guid Id { get; set; }
        public required DateTime ActualArrivalDate { get; set; }
        public string? Observations { get; set; }
    }
}
