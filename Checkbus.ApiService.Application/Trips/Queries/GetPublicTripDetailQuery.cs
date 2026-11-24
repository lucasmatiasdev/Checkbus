using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{

    public class GetPublicTripDetailQuery : IRequest<PublicTripDetailDto>
    {
        public required Guid TripId { get; set; }
    }
}
