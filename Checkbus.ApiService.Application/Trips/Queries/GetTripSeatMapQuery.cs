using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{

    public class GetTripSeatMapQuery : IRequest<TripSeatMapDto>
    {
        public required Guid TripId { get; set; }
    }
}
