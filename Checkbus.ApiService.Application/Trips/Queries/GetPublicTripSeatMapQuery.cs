using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{

    public class GetPublicTripSeatMapQuery : IRequest<PublicTripSeatMapDto>
    {
        public required Guid TripId { get; set; }
    }
}
