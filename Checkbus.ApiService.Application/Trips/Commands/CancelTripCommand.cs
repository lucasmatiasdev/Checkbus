using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{

    public class CancelTripCommand : IRequest
    {
        public required Guid Id { get; set; }
        public required TripCancellationReason Reason { get; set; }

        public string? Description { get; set; }
    }
}
