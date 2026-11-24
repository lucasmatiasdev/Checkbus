using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{

    public class GetTripLocationQuery : IRequest<TripLocationDto>
    {
        public required Guid TripId { get; set; }
    }
}
