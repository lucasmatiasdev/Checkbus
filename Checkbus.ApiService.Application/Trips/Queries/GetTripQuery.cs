using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    public class GetTripQuery : IRequest<TripDetailDto>
    {
        public required Guid Id { get; set; }
    }
}
