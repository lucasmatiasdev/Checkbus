using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    public class GetTripBoardingListQuery : IRequest<TripBoardingListDto>
    {
        public required Guid TripId { get; set; }
    }
}
